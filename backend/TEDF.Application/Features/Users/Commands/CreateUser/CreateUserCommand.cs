using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

namespace TEDF.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Admin creates a single user (Student/Mentor/Evaluator/DepartmentHead — never Admin) under the
/// SSO/pending model. Returns the new user id.
/// </summary>
[ActionLog("Create User", "User")]
public record CreateUserCommand(
    string Role,
    string Email,
    string FullName,
    string Code,
    string? Phone,
    string? AcademicTitle,
    int? MajorId
) : ICacheInvalidatingCommand<Guid>
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate => ["users:list:"];
}
