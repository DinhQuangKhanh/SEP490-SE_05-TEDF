using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Users.Commands.AssignDepartmentHead;

/// <summary>
/// Command for Admin to assign a lecturer as Head of Department (CNBM).
/// This will remove the DepartmentHead role from the previous head (if any)
/// and assign it to the new user.
/// </summary>
[ActionLog("Set Department Head", "Department")]
public record AssignDepartmentHeadCommand(
    int DepartmentId,
    Guid UserId
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}
