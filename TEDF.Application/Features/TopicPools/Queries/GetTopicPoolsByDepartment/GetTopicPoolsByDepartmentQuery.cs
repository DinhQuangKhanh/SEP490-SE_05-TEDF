using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPoolsByDepartment;

/// <summary>
/// Query to get all topic pools grouped by Department → Major hierarchy.
/// Used for the "kho đề tài theo khoa" view.
/// </summary>
public record GetTopicPoolsByDepartmentQuery : ICachedQuery<List<DepartmentWithPoolsDto>>
{
    public string? CacheKey => "topic-pools:by-department";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(5);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(30);
}
