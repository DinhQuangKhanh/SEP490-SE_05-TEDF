using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Semesters.DTOs;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Application.Features.Semesters.Queries.GetSemesterById;

/// <summary>
/// Handles GetSemesterByIdQuery by fetching a semester with phases and mapping to DTO.
/// </summary>
public class GetSemesterByIdQueryHandler : IQueryHandler<GetSemesterByIdQuery, SemesterDto>
{
    private readonly ISemesterRepository _semesterRepository;

    public GetSemesterByIdQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<SemesterDto> Handle(GetSemesterByIdQuery request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetWithPhasesAsync(request.SemesterId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Semester), request.SemesterId);

        return new SemesterDto
        {
            Id = semester.Id,
            Name = semester.Name,
            Code = semester.Code.Value,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            Status = semester.Status.ToString(),
            AcademicYear = semester.AcademicYear.Value,
            Description = semester.Description,
            CreatedAt = semester.CreatedAt,
            UpdatedAt = semester.UpdatedAt,
            Phases = semester.Phases.OrderBy(p => p.Order).Select(p => new SemesterPhaseDto
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
