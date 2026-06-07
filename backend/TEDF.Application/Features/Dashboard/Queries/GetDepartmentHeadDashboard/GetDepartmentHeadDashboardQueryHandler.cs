using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;

public class GetDepartmentHeadDashboardQueryHandler
    : IQueryHandler<GetDepartmentHeadDashboardQuery, DepartmentHeadDashboardDto>
{
    private readonly IDashboardQueryService _dashboard;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentHeadDashboardQueryHandler(IDashboardQueryService dashboard, ICurrentUserService currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    public Task<DepartmentHeadDashboardDto> Handle(GetDepartmentHeadDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _dashboard.GetDepartmentHeadDashboardAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
