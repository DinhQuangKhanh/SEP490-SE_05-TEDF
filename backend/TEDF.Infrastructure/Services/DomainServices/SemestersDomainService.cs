using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.Commands;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Semester;
using TEDF.Domain.Services;
using IDateTimeService = TEDF.Application.Common.Interfaces.IDateTimeService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Semesters feature. See <see cref="ISemestersDomainService"/>.
/// </summary>
public class SemestersDomainService : ISemestersDomainService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IExcelService _excelService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SemestersDomainService> _logger;

    public SemestersDomainService(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        IExcelService excelService,
        IDateTimeService dateTimeService,
        IUnitOfWork unitOfWork,
        ILogger<SemestersDomainService> logger)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _excelService = excelService;
        _dateTimeService = dateTimeService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ── Helper queries ─────────────────────────────────────────────────────
    public async Task<int?> GetActiveSemesterIdAsync(CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetActiveAsync(ct);
        return semester?.Id;
    }

    public async Task<int?> GetCurrentPhaseIdAsync(int semesterId, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
        var currentPhase = semester?.Phases.FirstOrDefault(p =>
            p.StartDate <= _dateTimeService.UtcNow && p.EndDate >= _dateTimeService.UtcNow);
        return currentPhase?.Id;
    }

    public async Task<bool> IsWithinPhaseAsync(int semesterId, int phaseId, DateTime date, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
        var phase = semester?.Phases.FirstOrDefault(p => p.Id == phaseId);
        if (phase is null) return false;
        return date >= phase.StartDate && date <= phase.EndDate;
    }

    public async Task<int?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken ct = default)
    {
        if (count <= 0) return semesterId;
        var targetSemester = await _semesterRepository.GetSemesterAfterAsync(semesterId, count, ct);
        return targetSemester?.Id;
    }

    // ── Write operations ───────────────────────────────────────────────────
    public async Task<int> CreateAsync(
        string name, string codeValue, DateTime startDate, DateTime endDate,
        int academicYearStart, string? description,
        IReadOnlyList<NewSemesterPhase> phases, CancellationToken ct = default)
    {
        var code = SemesterCode.Create(codeValue);
        if (await _semesterRepository.ExistsCodeAsync(code, ct))
            throw new BusinessRuleValidationException($"Semester code '{codeValue}' already exists.");

        if (await _semesterRepository.HasOverlappingAsync(startDate, endDate, cancellationToken: ct))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        var nextId = await _semesterRepository.GetNextIdAsync(ct);
        var academicYear = AcademicYear.Create(academicYearStart);

        var semester = Semester.Create(nextId, name, code, startDate, endDate, academicYear, description);

        // Registration & Evaluation must fall within the currently-active semester; Implementation &
        // Defense are validated against the new semester by the aggregate.
        var currentSemester = await _semesterRepository.GetActiveAsync(ct);

        foreach (var phaseDto in phases)
        {
            if (!Enum.TryParse<SemesterPhaseType>(phaseDto.Type, true, out var phaseType))
                throw new BusinessRuleValidationException($"Invalid phase type '{phaseDto.Type}'.");

            CurrentSemesterPhaseGuard.EnsureWithinCurrentSemester(
                currentSemester, phaseType, phaseDto.Name, phaseDto.StartDate, phaseDto.EndDate);

            semester.AddPhase(phaseDto.Name, phaseType, phaseDto.StartDate, phaseDto.EndDate);
        }

        await _semesterRepository.AddAsync(semester, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return semester.Id;
    }

    public async Task UpdateAsync(
        int id, string name, string? description, DateTime startDate, DateTime endDate,
        IReadOnlyList<SemesterPhaseDateChange>? phases, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), id);

        if (await _semesterRepository.HasOverlappingAsync(startDate, endDate, id, ct))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        semester.UpdateDetails(name, description);
        semester.UpdateDates(startDate, endDate);

        if (phases is { Count: > 0 })
        {
            var currentSemester = await _semesterRepository.GetActiveAsync(ct);

            foreach (var phase in phases)
            {
                var domainPhase = semester.Phases.FirstOrDefault(p => p.Id == phase.Id)
                    ?? throw new EntityNotFoundException(nameof(SemesterPhase), phase.Id);

                CurrentSemesterPhaseGuard.EnsureWithinCurrentSemester(
                    currentSemester, domainPhase.Type, domainPhase.Name, phase.StartDate, phase.EndDate);

                semester.UpdatePhaseDates(phase.Id, phase.StartDate, phase.EndDate);
            }
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), id);

        if (semester.Status != SemesterStatus.Upcoming)
            throw new BusinessRuleValidationException("Only upcoming semesters can be deleted.");

        _semesterRepository.Remove(semester);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<EligibleStudentsImportResult> ImportEligibleStudentsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken ct = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        if (fileStream == null || fileStream.Length == 0)
            throw new BusinessRuleValidationException("File tải lên không hợp lệ hoặc rỗng.");

        var studentCodes = await _excelService.ExtractStudentCodesAsync(fileStream, fileName, ct);

        if (!studentCodes.Any())
            throw new BusinessRuleValidationException("Không tìm thấy mã sinh viên nào trong file. Đảm bảo file có cột chứa mã sinh viên hợp lệ.");

        var successCount = 0;
        var failedCodes = new List<string>();

        foreach (var code in studentCodes)
        {
            var student = await _userRepository.GetByStudentCodeAsync(code, ct);
            if (student != null)
            {
                semester.AddEligibleStudent(student.Id, code, importedBy);
                successCount++;
            }
            else
            {
                failedCodes.Add(code);
            }
        }

        _semesterRepository.Update(semester);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Imported {Count} eligible students for Semester {SemesterId}. Failed: {FailedCount}",
            successCount, semesterId, failedCodes.Count);

        return new EligibleStudentsImportResult(studentCodes.Count, successCount, failedCodes);
    }
}
