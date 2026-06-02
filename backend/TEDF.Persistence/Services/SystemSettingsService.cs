using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.Services;

/// <summary>
/// Cached implementation of <see cref="ISystemSettingsService"/>. Reads the whole (small)
/// SystemConfiguration table once and caches it under the <c>settings:</c> prefix; the
/// UpdateSystemSettings command invalidates that prefix on write.
/// </summary>
public class SystemSettingsService : ISystemSettingsService
{
    internal const string CacheKey = "settings:all";

    private readonly ISystemConfigurationRepository _repository;
    private readonly ICacheService _cache;

    public SystemSettingsService(ISystemConfigurationRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrSetAsync(
            CacheKey,
            async () =>
            {
                var all = await _repository.GetAllAsync(ct);
                return all.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);
            },
            TimeSpan.FromMinutes(10),
            ct);
    }

    public async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.TryGetValue(key, out var v) ? v : defaultValue;
    }

    public Task InvalidateAsync(CancellationToken ct = default) => _cache.RemoveAsync(CacheKey, ct);
}
