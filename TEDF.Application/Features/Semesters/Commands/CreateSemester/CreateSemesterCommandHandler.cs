using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Application.Features.Semesters.Commands.CreateSemester;

/// <summary>
/// Handles the CreateSemesterCommand.
/// 1. Validates semester code uniqueness.
/// 2. Creates the Semester aggregate with phases.
/// 3. Persists via UnitOfWork.
/// </summary>
public class CreateSemesterCommandHandler : ICommandHandler<CreateSemesterCommand, int>
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSemesterCommandHandler(
        ISemesterRepository semesterRepository,
        IUnitOfWork unitOfWork)
    {
        _semesterRepository = semesterRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate code uniqueness
        var code = SemesterCode.Create(request.Code);
        if (await _semesterRepository.ExistsCodeAsync(code, cancellationToken))
            throw new BusinessRuleValidationException($"Semester code '{request.Code}' already exists.");

        // 2. Validate no overlapping semesters
        if (await _semesterRepository.HasOverlappingAsync(request.StartDate, request.EndDate, cancellationToken: cancellationToken))
            throw new BusinessRuleValidationException("Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.");

        // 3. Create semester aggregate
        var nextId = await _semesterRepository.GetNextIdAsync(cancellationToken);
        var academicYear = AcademicYear.Create(request.AcademicYearStart);

        var semester = Semester.Create(
            nextId,
            request.Name,
            code,
            request.StartDate,
            request.EndDate,
            academicYear,
            request.Description);

        // 3. Add phases
        foreach (var phaseDto in request.Phases)
        {
            if (!Enum.TryParse<SemesterPhaseType>(phaseDto.Type, true, out var phaseType))
                throw new BusinessRuleValidationException($"Invalid phase type '{phaseDto.Type}'.");

            semester.AddPhase(phaseDto.Name, phaseType, phaseDto.StartDate, phaseDto.EndDate);
        }

        // 4. Persist
        await _semesterRepository.AddAsync(semester, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return semester.Id;
    }
}
