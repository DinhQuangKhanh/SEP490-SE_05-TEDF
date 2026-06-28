using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetMySupervisedProjects;

public class GetMySupervisedProjectsQueryHandler
    : IQueryHandler<GetMySupervisedProjectsQuery, GetMySupervisedProjectsResult>
{
    private readonly IProjectsQueryService _projects;
    private readonly ICurrentUserService _currentUser;

    public GetMySupervisedProjectsQueryHandler(IProjectsQueryService projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public Task<GetMySupervisedProjectsResult> Handle(GetMySupervisedProjectsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return _projects.GetMySupervisedProjectsAsync(userId, cancellationToken);
    }
}
