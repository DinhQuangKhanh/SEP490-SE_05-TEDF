using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;

public class GetAdminDashboardQueryHandler : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IDashboardQueryService _dashboard;

    public GetAdminDashboardQueryHandler(IDashboardQueryService dashboard) => _dashboard = dashboard;

    public Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
        => _dashboard.GetAdminDashboardAsync(cancellationToken);
}
