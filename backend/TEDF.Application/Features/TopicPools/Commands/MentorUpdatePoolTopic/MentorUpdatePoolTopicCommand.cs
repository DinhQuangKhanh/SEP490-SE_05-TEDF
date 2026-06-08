using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.TopicPools.Commands.MentorUpdatePoolTopic;

public sealed record MentorUpdatePoolTopicCommand(
    Guid ProjectId,
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
    [
        $"topics:detail:{ProjectId}",
        "topics:list:",
        "topic-pools:",
        "mentor:"
    ];
}
