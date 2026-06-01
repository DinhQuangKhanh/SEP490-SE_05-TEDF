using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Command for a mentor to propose a new topic into a topic pool.
/// </summary>
[ActionLog("Propose Topic to Pool", "TopicPool")]
public record ProposeTopicToPoolCommand(
    Guid PoolId,
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents = 5
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["topic-pools:", "pool-topics:"];
}
