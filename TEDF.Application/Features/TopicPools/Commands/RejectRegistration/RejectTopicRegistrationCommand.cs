using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.TopicPools.Commands.RejectRegistration;

/// <summary>
/// Command to reject a topic registration request (used by admin or department head).
/// </summary>
/// <param name="RegistrationId">The ID of the registration to reject.</param>
/// <param name="Reason">The reason for rejection.</param>
[ActionLog("Reject Topic Registration", "TopicPool")]
public record RejectTopicRegistrationCommand(
    Guid RegistrationId,
    string Reason) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["topic-pools:"];
}
