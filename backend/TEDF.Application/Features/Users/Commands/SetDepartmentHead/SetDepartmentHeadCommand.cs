using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Users.Commands.SetDepartmentHead;

/// <summary>
/// Command for Admin to grant the Department Head role from the user-management screen, where only
/// the user is known — the department is resolved from the lecturer's own profile.
/// <para>
/// The role is a singleton: the current holder loses it as part of the same operation.
/// </para>
/// </summary>
[ActionLog("Set Department Head", "User")]
public record SetDepartmentHeadCommand(
    Guid UserId
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}
