using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;

public class GetAdminDashboardQueryHandler : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IAdminDashboardQueryService _queryService;

    public GetAdminDashboardQueryHandler(IAdminDashboardQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<AdminDashboardDto> Handle(
        GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        return await _queryService.GetDashboardAsync(cancellationToken);
    }
}
