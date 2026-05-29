using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces;

public interface IErrorLogRepository
{
    Task AddAsync(ErrorLogDocument log, CancellationToken ct = default);

    Task<(IEnumerable<ErrorLogDocument> Items, long TotalCount)> GetPagedAsync(
        string? severity = null,
        string? source = null,
        string? searchTerm = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ErrorLogDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<ErrorFrequencyResult>> GetTopErrorsAsync(
        int limit = 10,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}

public class ErrorFrequencyResult
{
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LatestAt { get; set; }
}
