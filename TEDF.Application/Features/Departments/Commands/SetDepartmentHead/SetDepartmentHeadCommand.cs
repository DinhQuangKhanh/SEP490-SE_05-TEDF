using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Departments.Commands.SetDepartmentHead;

/// <summary>
/// Command for Admin to assign a lecturer as Head of Department (CNBM).
/// This will remove the DepartmentHead role from the previous head (if any)
/// and assign it to the new user.
/// </summary>
[ActionLog("Set Department Head", "Department")]
public record SetDepartmentHeadCommand(
    int DepartmentId,
    Guid UserId
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}
