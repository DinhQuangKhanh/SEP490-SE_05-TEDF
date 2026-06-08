namespace TEDF.Infrastructure.Authentication
{
    /// <summary>
    /// Service interface for Firebase Authentication operations.
    /// </summary>
    public interface IFirebaseAuthService
    {
        /// <summary>
        /// Verifies a Firebase ID token and returns the user info.
        /// </summary>
        /// <param name="idToken">The Firebase ID token to verify.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The Firebase user info if valid, null otherwise.</returns>
        Task<FirebaseUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);

        /// <summary>
        /// Creates a custom token for a user with additional claims.
        /// </summary>
        /// <param name="userId">The user's internal ID.</param>
        /// <param name="claims">Additional claims to include in the token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A custom Firebase token.</returns>
        Task<string> CreateCustomTokenAsync(Guid userId, IDictionary<string, object>? claims = null, CancellationToken ct = default);

        /// <summary>
        /// Revokes all refresh tokens for a user.
        /// </summary>
        /// <param name="firebaseUid">The Firebase UID of the user.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RevokeRefreshTokensAsync(string firebaseUid, CancellationToken ct = default);

        /// <summary>
        /// Sets custom claims for a user in Firebase.
        /// </summary>
        /// <param name="firebaseUid">The Firebase UID of the user.</param>
        /// <param name="claims">The claims to set.</param>
        /// <param name="ct">Cancellation token.</param>
        Task SetCustomClaimsAsync(string firebaseUid, IReadOnlyDictionary<string, object> claims, CancellationToken ct = default);

        /// <summary>
        /// Disables a user account in Firebase.
        /// </summary>
        /// <param name="firebaseUid">The Firebase UID of the user.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DisableUserAsync(string firebaseUid, CancellationToken ct = default);

        /// <summary>
        /// Enables a user account in Firebase.
        /// </summary>
        /// <param name="firebaseUid">The Firebase UID of the user.</param>
        /// <param name="ct">Cancellation token.</param>
        Task EnableUserAsync(string firebaseUid, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents information about a Firebase authenticated user.
    /// </summary>
    public record FirebaseUserInfo(
        string Uid,
        string Email,
        string? DisplayName,
        string? PhotoUrl,
        bool EmailVerified
    );
}
