using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetProjects;

public class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, GetProjectsQueryResult>
{
    private readonly IProjectsQueryService _projects;

    public GetProjectsQueryHandler(IProjectsQueryService projects) => _projects = projects;

    public Task<GetProjectsQueryResult> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        => _projects.GetProjectsAsync(
            request.Search, request.SemesterId, request.Status, request.MajorId, request.Page, request.PageSize, cancellationToken);
}
