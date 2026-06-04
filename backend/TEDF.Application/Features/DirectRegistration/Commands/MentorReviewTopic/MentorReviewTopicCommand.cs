using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectRegistration.Commands.MentorReviewProposedTopic;

public record MentorReviewProposedTopicCommand(
    Guid ProjectId,
    string Action,
    string? Feedback
) : ICommand;
