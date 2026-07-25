using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLogDocument log, CancellationToken ct = default);

    Task<(IEnumerable<ActivityLogDocument> Items, long TotalCount)> GetPagedAsync(
        ActivityLogFilter filter,
        CancellationToken ct = default);

    /// <summary>Count of activity logs grouped by role — used for stat cards in admin dashboard.</summary>
    Task<Dictionary<string, long>> GetRoleCountsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    /// <summary>Count of Success vs Failure entries in a time range.</summary>
    Task<(long Success, long Failure)> GetStatusCountsAsync(
        string? role = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    /// <summary>Delete logs older than <paramref name="cutoff"/>. Pass null to delete all.</summary>
    Task<long> DeleteOlderThanAsync(DateTime? cutoff, CancellationToken ct = default);
}

/// <summary>Filter and paging options for <see cref="IActivityLogRepository.GetPagedAsync"/>.</summary>
public sealed record ActivityLogFilter(
    string? Role = null,
    string? FeatureCategory = null,
    string? Status = null,
    string? SearchTerm = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20);
