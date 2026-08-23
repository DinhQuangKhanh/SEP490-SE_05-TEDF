namespace TEDF.Domain.Services;

/// <summary>
/// Write-side maintenance of the pool over time, driven by the recurring Hangfire job: expire stale
/// topics and backfill missing expiration metadata. Split out of the old god-service
/// <c>ITopicPoolsDomainService</c> (single responsibility: pool lifecycle jobs).
/// </summary>
public interface IPoolLifecycleService
{
    /// <summary>
    /// Expires topics that have been in the pool for more than N semesters without registration.
    /// Should be called at the start of each semester. Returns the number expired.
    /// </summary>
    Task<int> ExpireOldTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backfills expiration semester for approved pool topics that are still missing expiration
    /// metadata. Returns the number resolved.
    /// </summary>
    Task<int> ResolveMissingExpirationSemestersAsync(CancellationToken cancellationToken = default);
}
