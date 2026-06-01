namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Provides access to information about the current authenticated user.
/// This is an Application layer concern as it deals with authentication context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the identifier of the current user.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the full name of the current user.
    /// </summary>
    string? FullName { get; }

    /// <summary>
    /// Gets the email of the current user.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the roles of the current user.
    /// </summary>
    IEnumerable<string> Roles { get; }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user is in the role; otherwise, false.</returns>
    bool IsInRole(string role);
}
