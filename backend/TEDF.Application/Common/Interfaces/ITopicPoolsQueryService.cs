namespace TEDF.Application.Common.Interfaces;

using TEDF.Application.Features.TopicPools.DTOs;

/// <summary>
/// Read-side query service for topic pool containers.
/// Handles pool-level queries only (pool metadata, statistics, department grouping).
/// For individual topic queries, see <see cref="ITopicsQueryService"/>.
/// </summary>
public interface ITopicPoolsQueryService
{
    Task<List<TopicPoolDto>> GetTopicPoolsAsync(int? majorId, CancellationToken cancellationToken = default);
    Task<List<DepartmentWithPoolsDto>> GetPoolsByDepartmentAsync(CancellationToken cancellationToken = default);
    Task<TopicPoolDto?> GetTopicPoolByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TopicPoolStatisticsDto> GetTopicPoolStatisticsAsync(Guid poolId, CancellationToken cancellationToken = default);
}
