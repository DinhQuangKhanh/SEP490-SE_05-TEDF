using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.CreateGroup;

[ActionLog("Create Group", "StudentGroup")]
public record CreateGroupCommand(string? Name) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
