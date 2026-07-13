using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Commands.MentorReviewTopic;

public record MentorReviewProposedTopicCommand(
    Guid ProjectId,
    string Action,
    string? Feedback
) : ICacheInvalidatingCommand
{
    // Approving / requesting-modification changes the project's status and persists the mentor's
    // feedback, so the cached topic detail (and lists) must be invalidated — otherwise the student's
    // page shows a stale reason/status until some other command flushes the cache.
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
    [
        $"topics:detail:{ProjectId}",
        "topics:list:",
        "topic-pools:"
    ];
}
