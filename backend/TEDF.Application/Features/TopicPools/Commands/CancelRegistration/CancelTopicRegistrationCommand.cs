using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.TopicPools.Commands.CancelRegistration;

/// <summary>
/// Command for a student group's leader to cancel their own pending topic registration.
/// </summary>
/// <param name="RegistrationId">The ID of the registration to cancel.</param>
/// <param name="Reason">Optional reason for cancelling.</param>
[ActionLog("Cancel Topic Registration", "TopicPool")]
public record CancelTopicRegistrationCommand(
    Guid RegistrationId,
    string? Reason = null) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["topic-pools:"];
}
