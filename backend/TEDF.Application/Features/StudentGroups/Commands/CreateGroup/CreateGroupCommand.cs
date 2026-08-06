using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.StudentGroups.Commands.CreateGroup;

[ActionLog("Create Group", "StudentGroup")]
/// <summary>
/// Creates the caller's group. It carries no name: the system assigns the id (SE_NN), and the
/// group is presented as "SE_NN - English topic name - Mentor" once its topic passes evaluation.
/// </summary>
public record CreateGroupCommand : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["student-groups:"];
}
