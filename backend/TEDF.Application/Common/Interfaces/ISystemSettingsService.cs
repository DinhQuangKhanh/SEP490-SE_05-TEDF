namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-optimized, cached access to the SystemConfiguration key/value store. Used by middleware
/// and command/query handlers that need to read settings cheaply. Writes go through the
/// ISystemConfigurationRepository + the UpdateSystemSettings command (which invalidates the cache).
/// </summary>
public interface ISystemSettingsService
{
    /// <summary>All settings as a key → raw-value map (cached).</summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default);

    Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default);
    Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default);
    Task<string> GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default);

    /// <summary>Clears the cached settings snapshot (call after a write).</summary>
    Task InvalidateAsync(CancellationToken ct = default);
}
