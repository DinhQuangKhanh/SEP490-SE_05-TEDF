namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Abstraction for managing external authentication provider accounts (e.g., Firebase).
/// Implemented in the Infrastructure layer.
/// </summary>
public interface IAuthAccountService
{
    /// <summary>
    /// Disables a user's authentication account, preventing them from signing in.
    /// </summary>
    Task DisableAccountAsync(string externalUid, CancellationToken ct = default);

    /// <summary>
    /// Re-enables a previously disabled user's authentication account.
    /// </summary>
    Task EnableAccountAsync(string externalUid, CancellationToken ct = default);
}
