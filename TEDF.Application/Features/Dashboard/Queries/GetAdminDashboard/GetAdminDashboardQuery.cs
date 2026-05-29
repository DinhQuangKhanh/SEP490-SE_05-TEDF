using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetAdminDashboard;

public record GetAdminDashboardQuery() : ICachedQuery<AdminDashboardDto>
{
    public string CacheKey => "admin:dashboard";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
