using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Commands.MentorReviewTopic;

public record MentorReviewProposedTopicCommand(
    Guid ProjectId,
    string Action,
    string? Feedback
) : ICommand;
