using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Authentication
{
    /// <summary>
    /// Firebase Authentication service implementation.
    /// Relies on FirebaseApp being initialized by DependencyInjection.AddInfrastructure().
    /// </summary>
    public class FirebaseAuthService : IFirebaseAuthService, IAuthAccountService
    {
        private readonly FirebaseSettings _settings;
        private readonly ILogger<FirebaseAuthService> _logger;
        private readonly FirebaseAuth _auth;

        public FirebaseAuthService(
            IOptions<FirebaseSettings> settings,
            ILogger<FirebaseAuthService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _auth = FirebaseAuth.DefaultInstance
                ?? throw new InvalidOperationException(
                    "FirebaseApp has not been initialized. Ensure AddInfrastructure() is called before resolving IFirebaseAuthService.");
        }

        /// <inheritdoc/>
        public async Task<FirebaseUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
        {
            try
            {
                var decodedToken = await _auth.VerifyIdTokenAsync(idToken, ct);

                return new FirebaseUserInfo(
                    Uid: decodedToken.Uid,
                    Email: decodedToken.Claims.TryGetValue("email", out var email) ? email?.ToString() ?? string.Empty : string.Empty,
                    DisplayName: decodedToken.Claims.TryGetValue("name", out var name) ? name?.ToString() : null,
                    PhotoUrl: decodedToken.Claims.TryGetValue("picture", out var picture) ? picture?.ToString() : null,
                    EmailVerified: decodedToken.Claims.TryGetValue("email_verified", out var verified) && verified is bool v && v
                );
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(ex, "Failed to verify Firebase ID token");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<string> CreateCustomTokenAsync(Guid userId, IDictionary<string, object>? claims = null, CancellationToken ct = default)
        {
            try
            {
                var customClaims = claims ?? new Dictionary<string, object>();
                customClaims["internalUserId"] = userId.ToString();

                return await _auth.CreateCustomTokenAsync(userId.ToString(), customClaims, ct);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to create custom token for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RevokeRefreshTokensAsync(string firebaseUid, CancellationToken ct = default)
        {
            try
            {
                await _auth.RevokeRefreshTokensAsync(firebaseUid, ct);
                _logger.LogInformation("Revoked refresh tokens for Firebase user {FirebaseUid}", firebaseUid);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to revoke refresh tokens for Firebase user {FirebaseUid}", firebaseUid);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task SetCustomClaimsAsync(string firebaseUid, IReadOnlyDictionary<string, object> claims, CancellationToken ct = default)
        {
            try
            {
                await _auth.SetCustomUserClaimsAsync(firebaseUid, claims, ct);
                _logger.LogInformation("Set custom claims for Firebase user {FirebaseUid}", firebaseUid);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to set custom claims for Firebase user {FirebaseUid}", firebaseUid);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DisableUserAsync(string firebaseUid, CancellationToken ct = default)
        {
            try
            {
                var args = new UserRecordArgs
                {
                    Uid = firebaseUid,
                    Disabled = true
                };

                await _auth.UpdateUserAsync(args, ct);
                _logger.LogInformation("Disabled Firebase user {FirebaseUid}", firebaseUid);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to disable Firebase user {FirebaseUid}", firebaseUid);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task EnableUserAsync(string firebaseUid, CancellationToken ct = default)
        {
            try
            {
                var args = new UserRecordArgs
                {
                    Uid = firebaseUid,
                    Disabled = false
                };

                await _auth.UpdateUserAsync(args, ct);
                _logger.LogInformation("Enabled Firebase user {FirebaseUid}", firebaseUid);
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Failed to enable Firebase user {FirebaseUid}", firebaseUid);
                throw;
            }
        }

        /// <inheritdoc cref="IAuthAccountService.DisableAccountAsync"/>
        public Task DisableAccountAsync(string externalUid, CancellationToken ct = default)
            => DisableUserAsync(externalUid, ct);

        /// <inheritdoc cref="IAuthAccountService.EnableAccountAsync"/>
        public Task EnableAccountAsync(string externalUid, CancellationToken ct = default)
            => EnableUserAsync(externalUid, ct);
    }
}
