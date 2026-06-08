namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the DirectTopics feature (student-initiated topics + mentor review).
/// Command handlers depend on this only.
/// </summary>
public interface IDirectTopicsDomainService
{
    Task<Guid> CreateDirectTopicAsync(Guid createdBy, Guid groupId, Guid mentorId, int majorId, DirectTopicContent content, CancellationToken cancellationToken = default);
    Task UpdateDirectTopicAsync(Guid projectId, DirectTopicContent content, CancellationToken cancellationToken = default);
    Task SubmitToMentorAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task MentorReviewAsync(Guid projectId, Guid mentorUserId, string action, string? feedback, CancellationToken cancellationToken = default);
}

/// <summary>Editable content of a direct topic.</summary>
public record DirectTopicContent(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
);
