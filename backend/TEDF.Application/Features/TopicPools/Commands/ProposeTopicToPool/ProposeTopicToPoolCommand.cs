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
    /// <summary>
    /// The capstone register form (PDF or DOCX). Attaching it is required; a form listing students
    /// additionally seeds the topic's proposed roster, which becomes a group once the topic passes
    /// evaluation. Declared ahead of <paramref name="MaxStudents"/> because a required positional
    /// parameter cannot follow one with a default value.
    /// </summary>
    byte[] RegisterForm,
    int MaxStudents = 5
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["topic-pools:", "pool-topics:"];
}
