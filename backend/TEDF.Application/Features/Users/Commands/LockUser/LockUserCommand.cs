using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Users.Commands.LockUser;

/// <summary>
/// Command to lock a user account, disabling their access via both database and Firebase.
/// Invalidates user list cache after successful execution.
/// </summary>
[ActionLog("Lock User", "User")]
public record LockUserCommand(Guid UserId) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["users:list:"];
}
