using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.DisbandGroup;

[ActionLog("Disband Group", "StudentGroup")]
public record DisbandGroupCommand(Guid GroupId) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
