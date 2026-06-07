using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;

[ActionLog("Respond Invitation", "StudentGroup")]
public record RespondInvitationCommand(Guid GroupId, int InvitationId, bool Accept) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
