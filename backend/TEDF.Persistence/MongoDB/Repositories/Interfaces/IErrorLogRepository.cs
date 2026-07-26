using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces;

public interface IErrorLogRepository
{
    Task AddAsync(ErrorLogDocument log, CancellationToken ct = default);

    Task<(IEnumerable<ErrorLogDocument> Items, long TotalCount)> GetPagedAsync(
        ErrorLogFilter filter,
        CancellationToken ct = default);

    Task<ErrorLogDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<ErrorFrequencyResult>> GetTopErrorsAsync(
        int limit = 10,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);

    /// <summary>Delete error logs older than <paramref name="cutoff"/>. Pass null to delete all.</summary>
    Task<long> DeleteOlderThanAsync(DateTime? cutoff, CancellationToken ct = default);
}

/// <summary>Filter and paging options for <see cref="IErrorLogRepository.GetPagedAsync"/>.</summary>
public sealed record ErrorLogFilter(
    string? Severity = null,
    string? Source = null,
    string? SearchTerm = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20);

public class ErrorFrequencyResult
{
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LatestAt { get; set; }
}
