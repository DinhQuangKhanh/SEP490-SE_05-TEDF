using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Common.Interfaces;

public interface IAdminDashboardQueryService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
