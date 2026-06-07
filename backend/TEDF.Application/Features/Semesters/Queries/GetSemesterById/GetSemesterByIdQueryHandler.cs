using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetSemesterById;

public class GetSemesterByIdQueryHandler : IQueryHandler<GetSemesterByIdQuery, SemesterDto>
{
    private readonly ISemestersQueryService _semesters;

    public GetSemesterByIdQueryHandler(ISemestersQueryService semesters) => _semesters = semesters;

    public Task<SemesterDto> Handle(GetSemesterByIdQuery request, CancellationToken cancellationToken)
        => _semesters.GetByIdAsync(request.SemesterId, cancellationToken);
}
