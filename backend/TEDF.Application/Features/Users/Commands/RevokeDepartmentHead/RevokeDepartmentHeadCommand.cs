using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Users.Commands.RevokeDepartmentHead;

/// <summary>
/// Command for Admin to take the Department Head role back, leaving the lecturer with their
/// Mentor/Evaluator roles. Also clears the head pointer on the department they led.
/// </summary>
[ActionLog("Revoke Department Head", "User")]
public record RevokeDepartmentHeadCommand(
    Guid UserId
) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}
