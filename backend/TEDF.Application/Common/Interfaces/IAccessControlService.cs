namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Evaluates whether a user may currently use the system: account status (locked/inactive) and,
/// for student-only accounts, eligibility on the active-or-upcoming semester roster.
/// </summary>
public interface IAccessControlService
{
    Task<AccessDecision> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an access evaluation. <see cref="Kind"/> is null when allowed, otherwise one of
/// "locked" | "inactive" | "student_not_eligible".
/// </summary>
public record AccessDecision(bool Allowed, string? Kind, string? Reason);
