using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;

[ActionLog("Respond Join Request", "StudentGroup")]
public record RespondJoinRequestCommand(Guid GroupId, int RequestId, bool Approve) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
