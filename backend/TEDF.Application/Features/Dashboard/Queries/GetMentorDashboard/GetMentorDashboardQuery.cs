using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetMentorDashboard;

public record GetMentorDashboardQuery() : ICachedQuery<MentorDashboardDto>
{
    public string CacheKey => "mentor:{userId}:dashboard";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(1);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(5);
}
