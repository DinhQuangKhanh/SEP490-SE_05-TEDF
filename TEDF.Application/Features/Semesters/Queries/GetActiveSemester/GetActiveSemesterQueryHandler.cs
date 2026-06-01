using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Domain.Aggregates.SemesterAggregate;

namespace TEDF.Application.Features.Semesters.Queries.GetActiveSemester;

/// <summary>
/// Handles GetActiveSemesterQuery by fetching the currently active semester.
/// Returns null if no active semester exists.
/// </summary>
public class GetActiveSemesterQueryHandler : IQueryHandler<GetActiveSemesterQuery, SemesterDto?>
{
    private readonly ISemesterRepository _semesterRepository;

    public GetActiveSemesterQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<SemesterDto?> Handle(GetActiveSemesterQuery request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetActiveAsync(cancellationToken);

        if (semester is null)
            return null;

        // Load phases
        var semesterWithPhases = await _semesterRepository.GetWithPhasesAsync(semester.Id, cancellationToken);
        if (semesterWithPhases is null)
            return null;

        return new SemesterDto
        {
            Id = semesterWithPhases.Id,
            Name = semesterWithPhases.Name,
            Code = semesterWithPhases.Code.Value,
            StartDate = semesterWithPhases.StartDate,
            EndDate = semesterWithPhases.EndDate,
            Status = semesterWithPhases.Status.ToString(),
            AcademicYear = semesterWithPhases.AcademicYear.Value,
            Description = semesterWithPhases.Description,
            CreatedAt = semesterWithPhases.CreatedAt,
            UpdatedAt = semesterWithPhases.UpdatedAt,
            Phases = semesterWithPhases.Phases.OrderBy(p => p.Order).Select(p => new SemesterPhaseDto
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
}
