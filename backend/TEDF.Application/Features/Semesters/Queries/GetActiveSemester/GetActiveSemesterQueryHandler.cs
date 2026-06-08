using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetActiveSemester;

public class GetActiveSemesterQueryHandler : IQueryHandler<GetActiveSemesterQuery, SemesterDto?>
{
    private readonly ISemestersQueryService _semesters;

    public GetActiveSemesterQueryHandler(ISemestersQueryService semesters) => _semesters = semesters;

    public Task<SemesterDto?> Handle(GetActiveSemesterQuery request, CancellationToken cancellationToken)
        => _semesters.GetActiveAsync(cancellationToken);
}
