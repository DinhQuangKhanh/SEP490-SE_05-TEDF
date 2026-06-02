using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.TopicPools.Commands.RequestRegistration;

/// <summary>
/// Command for a student group to request registration for a topic from the pool.
/// </summary>
/// <param name="ProjectId">The ID of the pool topic (project) to register for.</param>
/// <param name="GroupId">The ID of the student group making the request.</param>
/// <param name="Note">Optional note from the group.</param>
[ActionLog("Request Topic Registration", "TopicPool")]
public record RequestTopicRegistrationCommand(
    Guid ProjectId,
    Guid GroupId,
    string? Note = null) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["topic-pools:"];
}
