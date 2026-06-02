namespace TEDF.Application.Common;

/// <summary>
/// Custom JWT claim type keys added by this application on top of Firebase's standard claims.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>
    /// The database primary key (Guid) of the authenticated user in the Users table.
    /// Injected during JWT token validation by looking up the Firebase UID in the UserRepository.
    /// </summary>
    public const string DbUserId = "Id";
}
