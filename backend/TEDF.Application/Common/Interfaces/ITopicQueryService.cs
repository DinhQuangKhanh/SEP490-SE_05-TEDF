using TEDF.Application.Features.Topics.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side query service for individual thesis topics (projects).
/// Covers all topics regardless of source type (FromPool or DirectRegistration).
/// </summary>
public interface ITopicQueryService
{
    /// <summary>
    /// Returns a paginated list of topics available in the topic pool for student browsing.
    /// Only includes FromPool topics (SourceType = 0).
    /// </summary>
    Task<GetTopicsInPoolResult> GetTopicsInPoolAsync(
        int? majorId, string? search, int? poolStatus, string? sortBy,
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full details of a single topic by its ID.
    /// Works for all source types: FromPool and DirectRegistration.
    /// </summary>
    Task<TopicDetailDto?> GetTopicDetailAsync(Guid topicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns documents attached to a topic/project (non-deleted only).
    /// </summary>
    Task<List<TopicDocumentDto>> GetTopicDocumentsAsync(Guid topicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of topics assigned to a specific mentor.
    /// </summary>
    Task<GetMentorTopicsResult> GetMentorTopicsAsync(
        Guid mentorId, int? semesterId, string? search,
        int page, int pageSize, CancellationToken cancellationToken = default);
}
