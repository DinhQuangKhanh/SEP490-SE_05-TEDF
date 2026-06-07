using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.InviteMember;

[ActionLog("Invite Member", "StudentGroup")]
public record InviteMemberCommand(Guid GroupId, string StudentCode, string? Message) : ICacheInvalidatingCommand<int>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
