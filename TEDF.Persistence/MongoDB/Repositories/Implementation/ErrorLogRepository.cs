using MongoDB.Bson;
using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly IMongoCollection<ErrorLogDocument> _collection;

    public ErrorLogRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<ErrorLogDocument>(MongoDbContext.Collections.ErrorLogs);
    }

    public async Task AddAsync(ErrorLogDocument log, CancellationToken ct = default)
        => await _collection.InsertOneAsync(log, cancellationToken: ct);

    public async Task<ErrorLogDocument?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _collection.Find(l => l.Id == id).FirstOrDefaultAsync(ct);

    public async Task<(IEnumerable<ErrorLogDocument> Items, long TotalCount)> GetPagedAsync(
        string? severity = null,
        string? source = null,
        string? searchTerm = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var builder = Builders<ErrorLogDocument>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(severity))
            filter &= builder.Eq(l => l.Severity, severity);

        if (!string.IsNullOrEmpty(source))
            filter &= builder.Eq(l => l.Source, source);

        if (from.HasValue)
            filter &= builder.Gte(l => l.Timestamp, from.Value);

        if (to.HasValue)
            filter &= builder.Lte(l => l.Timestamp, to.Value);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var regex = new BsonRegularExpression(searchTerm, "i");
            filter &= builder.Or(
                builder.Regex(l => l.ErrorMessage, regex),
                builder.Regex(l => l.ErrorType, regex),
                builder.Regex(l => l.Action, regex),
                builder.Regex(l => l.UserName, regex)
            );
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _collection
            .Find(filter)
            .SortByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<ErrorFrequencyResult>> GetTopErrorsAsync(
        int limit = 10,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var builder = Builders<ErrorLogDocument>.Filter;
        var filter = builder.Empty;

        if (from.HasValue)
            filter &= builder.Gte(l => l.Timestamp, from.Value);
        if (to.HasValue)
            filter &= builder.Lte(l => l.Timestamp, to.Value);

        var pipeline = _collection.Aggregate()
            .Match(filter)
            .Group(
                l => new { l.ErrorType, l.ErrorMessage },
                g => new ErrorFrequencyResult
                {
                    ErrorType = g.Key.ErrorType,
                    Message = g.Key.ErrorMessage,
                    Count = g.Count(),
                    LatestAt = g.Max(l => l.Timestamp)
                })
            .SortByDescending(r => r.Count)
            .Limit(limit);

        return await pipeline.ToListAsync(ct);
    }
}
