using MongoDB.Bson;
using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly IMongoCollection<ActivityLogDocument> _collection;

    public ActivityLogRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<ActivityLogDocument>(MongoDbContext.Collections.ActivityLogs);
    }

    public async Task AddAsync(ActivityLogDocument log, CancellationToken ct = default)
        => await _collection.InsertOneAsync(log, cancellationToken: ct);

    public async Task<(IEnumerable<ActivityLogDocument> Items, long TotalCount)> GetPagedAsync(
        string? role = null,
        string? featureCategory = null,
        string? status = null,
        string? searchTerm = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var builder = Builders<ActivityLogDocument>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(role))
            filter &= builder.Regex(l => l.Role, new BsonRegularExpression(role, "i"));

        if (!string.IsNullOrEmpty(featureCategory))
            filter &= builder.Eq(l => l.FeatureCategory, featureCategory);

        if (!string.IsNullOrEmpty(status))
            filter &= builder.Eq(l => l.Status, status);

        if (from.HasValue)
            filter &= builder.Gte(l => l.Timestamp, from.Value);

        if (to.HasValue)
            filter &= builder.Lte(l => l.Timestamp, to.Value);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var regex = new BsonRegularExpression(searchTerm, "i");
            filter &= builder.Or(
                builder.Regex(l => l.UserName, regex),
                builder.Regex(l => l.UserEmail, regex),
                builder.Regex(l => l.ActionName, regex),
                builder.Regex(l => l.ActionCode, regex),
                builder.Regex(l => l.EntityType, regex)
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

    public async Task<Dictionary<string, long>> GetRoleCountsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var builder = Builders<ActivityLogDocument>.Filter;
        var filter = builder.Empty;

        if (from.HasValue) filter &= builder.Gte(l => l.Timestamp, from.Value);
        if (to.HasValue)   filter &= builder.Lte(l => l.Timestamp, to.Value);

        var result = await _collection.Aggregate()
            .Match(filter)
            .Group(l => l.Role, g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return result.ToDictionary(r => r.Role, r => (long)r.Count);
    }

    public async Task<(long Success, long Failure)> GetStatusCountsAsync(
        string? role = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var builder = Builders<ActivityLogDocument>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(role))
            filter &= builder.Regex(l => l.Role, new BsonRegularExpression(role, "i"));
        if (from.HasValue) filter &= builder.Gte(l => l.Timestamp, from.Value);
        if (to.HasValue)   filter &= builder.Lte(l => l.Timestamp, to.Value);

        var successFilter = filter & builder.Eq(l => l.Status, "Success");
        var failureFilter = filter & builder.Eq(l => l.Status, "Failure");

        var successCount = await _collection.CountDocumentsAsync(successFilter, cancellationToken: ct);
        var failureCount = await _collection.CountDocumentsAsync(failureFilter, cancellationToken: ct);

        return (successCount, failureCount);
    }

    public async Task<long> DeleteOlderThanAsync(DateTime? cutoff, CancellationToken ct = default)
    {
        var filter = cutoff.HasValue
            ? Builders<ActivityLogDocument>.Filter.Lt(l => l.Timestamp, cutoff.Value)
            : Builders<ActivityLogDocument>.Filter.Empty;

        var result = await _collection.DeleteManyAsync(filter, cancellationToken: ct);
        return result.DeletedCount;
    }
}
