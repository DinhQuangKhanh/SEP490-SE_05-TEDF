using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Command for a mentor to propose a new topic into a topic pool by uploading the completed
/// "Capstone Project Register" form. The 3.1–3.4 content is read from the form; the mentor may
/// correct it in the modal (<see cref="EditedContent"/>). The only free-text extra is the optional
/// mentor note (sanitized rich-text HTML from the modal).
/// </summary>
[ActionLog("Propose Topic to Pool", "TopicPool")]
public record ProposeTopicToPoolCommand(
    Guid PoolId,
    /// <summary>The capstone register form (PDF / DOCX / DOC). Parsed for the topic content + roster.</summary>
    byte[] RegisterForm,
    /// <summary>Optional mentor note (sanitized HTML) — e.g. capability requirements for registrants.</summary>
    string? Note = null,
    /// <summary>Mentor's corrections to the parsed 3.1–3.4 content (null = keep what the form said).</summary>
    RegisterFormContentEdit? EditedContent = null
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["topic-pools:", "pool-topics:"];
}
