using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Semesters feature. See <see cref="ISemestersQueryService"/>.
/// </summary>
public class SemestersQueryService : ISemestersQueryService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMajorReadRepository _majorRepository;

    public SemestersQueryService(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        IMajorReadRepository majorRepository)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _majorRepository = majorRepository;
    }

    public async Task<SemesterDto?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetActiveAsync(cancellationToken);
        if (semester is null) return null;

        var withPhases = await _semesterRepository.GetWithPhasesAsync(semester.Id, cancellationToken);
        return withPhases is null ? null : MapToDto(withPhases);
    }

    public async Task<List<SemesterDto>> GetAllAsync(string? status, CancellationToken cancellationToken = default)
    {
        var semesters = await _semesterRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SemesterStatus>(status, true, out var statusFilter))
        {
            var now = DateTime.UtcNow;
            semesters = statusFilter switch
            {
                SemesterStatus.Upcoming => semesters.Where(s => s.StartDate > now),
                SemesterStatus.Ended => semesters.Where(s => s.EndDate < now),
                SemesterStatus.Ongoing => semesters.Where(s => s.StartDate <= now && s.EndDate >= now),
                _ => semesters
            };
        }

        return semesters.Select(MapToDto).ToList();
    }

    public async Task<SemesterDto> GetByIdAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        return MapToDto(semester);
    }

    public async Task<List<EligibleStudentDto>> GetEligibleStudentsAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        var rows = semester.EligibleStudents.ToList();
        if (rows.Count == 0) return [];

        var users = (await _userRepository.GetByIdsAsync(rows.Select(r => r.StudentId).Distinct(), cancellationToken))
            .ToDictionary(u => u.Id);
        var majors = (await _majorRepository.GetAllAsync(cancellationToken)).ToDictionary(m => m.Id);

        return rows
            .OrderBy(r => r.StudentCode)
            .Select(r =>
            {
                users.TryGetValue(r.StudentId, out var user);
                var major = r.MajorId.HasValue && majors.TryGetValue(r.MajorId.Value, out var m) ? m : null;
                return new EligibleStudentDto
                {
                    StudentId = r.StudentId,
                    StudentCode = r.StudentCode,
                    FullName = user?.FullName,
                    Email = r.Email ?? user?.Email.Value,
                    PhoneNumber = r.PhoneNumber,
                    MajorId = r.MajorId,
                    ProgramCode = major?.Code,
                    ProgramName = major?.Name,
                    IsEligible = r.IsEligible,
                    ImportedAt = r.ImportedAt
                };
            })
            .ToList();
    }

    public async Task<List<EligibleMentorDto>> GetEligibleMentorsAsync(int semesterId, CancellationToken cancellationToken = default)
    {
        var semester = await _semesterRepository.GetWithRosterAsync(semesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), semesterId);

        var rows = semester.EligibleMentors.ToList();
        if (rows.Count == 0) return [];

        var users = (await _userRepository.GetByIdsAsync(rows.Select(r => r.MentorId).Distinct(), cancellationToken))
            .ToDictionary(u => u.Id);
        var majors = (await _majorRepository.GetAllAsync(cancellationToken)).ToDictionary(m => m.Id);

        return rows
            .OrderBy(r => r.EmployeeCode)
            .Select(r =>
            {
                users.TryGetValue(r.MentorId, out var user);
                var major = r.MajorId is int mid && majors.TryGetValue(mid, out var m) ? m : null;
                return new EligibleMentorDto
                {
                    MentorId = r.MentorId,
                    EmployeeCode = r.EmployeeCode,
                    FullName = user?.FullName,
                    Email = r.Email ?? user?.Email.Value,
                    PhoneNumber = r.PhoneNumber,
                    Division = r.Division,
                    MajorId = r.MajorId,
                    ProgramCode = major?.Code,
                    ProgramName = major?.Name,
                    IsAssigned = r.IsAssigned,
                    ImportedAt = r.ImportedAt
                };
            })
            .ToList();
    }

    private static SemesterDto MapToDto(Semester s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code.Value,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        Status = s.Status.ToString(),
        AcademicYear = s.AcademicYear.Value,
        Description = s.Description,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        RosterPublishedAt = s.RosterPublishedAt,
        Phases = s.Phases.OrderBy(p => p.Order).Select(p => new SemesterPhaseDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type.ToString(),
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Order = p.Order,
            Status = p.Status.ToString(),
            DurationDays = p.DurationDays
        }).ToList()
    };
}
