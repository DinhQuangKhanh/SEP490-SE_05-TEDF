using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.LeaveGroup;

[ActionLog("Leave Group", "StudentGroup")]
public record LeaveGroupCommand(Guid GroupId) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
