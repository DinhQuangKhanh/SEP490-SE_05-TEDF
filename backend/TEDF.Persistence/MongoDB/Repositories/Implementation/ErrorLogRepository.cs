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
        ErrorLogFilter filter,
        CancellationToken ct = default)
    {
        var builder = Builders<ErrorLogDocument>.Filter;
        var mongoFilter = builder.Empty;

        if (!string.IsNullOrEmpty(filter.Severity))
            mongoFilter &= builder.Eq(l => l.Severity, filter.Severity);

        if (!string.IsNullOrEmpty(filter.Source))
            mongoFilter &= builder.Eq(l => l.Source, filter.Source);

        if (filter.From.HasValue)
            mongoFilter &= builder.Gte(l => l.Timestamp, filter.From.Value);

        if (filter.To.HasValue)
            mongoFilter &= builder.Lte(l => l.Timestamp, filter.To.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var regex = new BsonRegularExpression(filter.SearchTerm, "i");
            mongoFilter &= builder.Or(
                builder.Regex(l => l.ErrorMessage, regex),
                builder.Regex(l => l.ErrorType, regex),
                builder.Regex(l => l.Action, regex),
                builder.Regex(l => l.UserName, regex)
            );
        }

        var totalCount = await _collection.CountDocumentsAsync(mongoFilter, cancellationToken: ct);

        var items = await _collection
            .Find(mongoFilter)
            .SortByDescending(l => l.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
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

    public async Task<long> DeleteOlderThanAsync(DateTime? cutoff, CancellationToken ct = default)
    {
        var filter = cutoff.HasValue
            ? Builders<ErrorLogDocument>.Filter.Lt(l => l.Timestamp, cutoff.Value)
            : Builders<ErrorLogDocument>.Filter.Empty;

        var result = await _collection.DeleteManyAsync(filter, cancellationToken: ct);
        return result.DeletedCount;
    }
}
