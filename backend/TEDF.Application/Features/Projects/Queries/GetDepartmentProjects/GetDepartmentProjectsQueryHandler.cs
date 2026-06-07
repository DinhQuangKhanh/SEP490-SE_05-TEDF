using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentProjects;

public class GetDepartmentProjectsQueryHandler : IQueryHandler<GetDepartmentProjectsQuery, DepartmentProjectsResponse>
{
    private readonly IProjectsQueryService _projects;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentProjectsQueryHandler(IProjectsQueryService projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public Task<DepartmentProjectsResponse> Handle(GetDepartmentProjectsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _projects.GetDepartmentProjectsAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
