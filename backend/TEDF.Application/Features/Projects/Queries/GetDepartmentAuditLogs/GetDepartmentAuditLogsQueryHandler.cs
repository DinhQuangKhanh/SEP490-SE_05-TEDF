using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentAuditLogs;

public class GetDepartmentAuditLogsQueryHandler
    : IQueryHandler<GetDepartmentAuditLogsQuery, GetDepartmentAuditLogsResponse>
{
    private readonly IProjectsQueryService _projects;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentAuditLogsQueryHandler(IProjectsQueryService projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public Task<GetDepartmentAuditLogsResponse> Handle(
        GetDepartmentAuditLogsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _projects.GetDepartmentAuditLogsAsync(
            _currentUser.UserId.Value, request.Search, request.Actions,
            request.Page, request.PageSize, cancellationToken);
    }
}
