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

    /// <summary>
    /// Gets all topic-pool registrations made by a group (newest first), with resolved
    /// topic name/code, mentor name and status.
    /// </summary>
    Task<List<GroupRegistrationDto>> GetGroupRegistrationsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the pending registration requests for a mentor's pool topics (newest first),
    /// with resolved topic, group and requester info.
    /// </summary>
    Task<List<MentorRegistrationRequestDto>> GetMentorRegistrationsAsync(Guid mentorId, CancellationToken cancellationToken = default);
}
