using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectRegistration.Commands.MentorReviewTopic;

public record MentorReviewTopicCommand(
    Guid ProjectId,
    string Action,
    string? Feedback
) : ICommand;
