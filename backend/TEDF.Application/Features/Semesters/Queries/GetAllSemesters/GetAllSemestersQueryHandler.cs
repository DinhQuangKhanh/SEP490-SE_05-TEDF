using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetAllSemesters;

public class GetAllSemestersQueryHandler : IQueryHandler<GetAllSemestersQuery, List<SemesterDto>>
{
    private readonly ISemestersQueryService _semesters;

    public GetAllSemestersQueryHandler(ISemestersQueryService semesters) => _semesters = semesters;

    public Task<List<SemesterDto>> Handle(GetAllSemestersQuery request, CancellationToken cancellationToken)
        => _semesters.GetAllAsync(request.Status, cancellationToken);
}
