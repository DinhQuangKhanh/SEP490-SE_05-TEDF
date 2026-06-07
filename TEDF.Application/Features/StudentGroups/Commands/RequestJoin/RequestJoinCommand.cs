using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.RequestJoin;

[ActionLog("Request Join", "StudentGroup")]
public record RequestJoinCommand(Guid GroupId, string? Message) : ICacheInvalidatingCommand<int>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
