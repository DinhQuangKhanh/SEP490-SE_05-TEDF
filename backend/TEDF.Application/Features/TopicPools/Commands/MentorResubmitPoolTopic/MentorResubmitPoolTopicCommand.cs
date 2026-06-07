using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.TopicPools.Commands.MentorResubmitPoolTopic;

public sealed record MentorResubmitPoolTopicCommand(Guid ProjectId) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
    [
        $"topics:detail:{ProjectId}",
        "topics:list:",
        "topic-pools:",
        "mentor:",
        "evaluator:"
    ];
}
