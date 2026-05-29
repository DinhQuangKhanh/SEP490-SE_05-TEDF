using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Application.Features.Semesters.Queries.GetAllSemesters;

/// <summary>
/// Handles GetAllSemestersQuery by fetching all semesters and mapping to DTOs.
/// </summary>
public class GetAllSemestersQueryHandler : IQueryHandler<GetAllSemestersQuery, List<SemesterDto>>
{
    private readonly ISemesterRepository _semesterRepository;

    public GetAllSemestersQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<List<SemesterDto>> Handle(GetAllSemestersQuery request, CancellationToken cancellationToken)
    {
        var semesters = await _semesterRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<SemesterStatus>(request.Status, true, out var statusFilter))
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

        return semesters.Select(s => new SemesterDto
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
        }).ToList();
    }
}
