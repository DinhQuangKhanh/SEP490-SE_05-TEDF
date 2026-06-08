using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Semesters feature. See <see cref="ISemestersQueryService"/>.
/// </summary>
public class SemestersQueryService : ISemestersQueryService
{
    private readonly ISemesterRepository _semesterRepository;

    public SemestersQueryService(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
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
