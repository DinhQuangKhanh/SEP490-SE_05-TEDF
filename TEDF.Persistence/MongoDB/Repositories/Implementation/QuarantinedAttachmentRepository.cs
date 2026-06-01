using MongoDB.Driver;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Persistence.MongoDB.Repositories.Implementation;

public class QuarantinedAttachmentRepository : IQuarantinedAttachmentRepository
{
    private readonly IMongoCollection<QuarantinedAttachmentDocument> _collection;

    public QuarantinedAttachmentRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<QuarantinedAttachmentDocument>("quarantined_attachments");
    }

    public Task AddAsync(QuarantinedAttachmentDocument document, CancellationToken ct = default)
        => _collection.InsertOneAsync(document, cancellationToken: ct);

    public Task DeleteByQuarantinePathAsync(string quarantinePath, CancellationToken ct = default)
        => _collection.DeleteOneAsync(d => d.QuarantinePath == quarantinePath, ct);

    public async Task<IReadOnlyList<QuarantinedAttachmentDocument>> GetStaleAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow - olderThan;
        return await _collection
            .Find(d => d.QueuedAt < threshold)
            .ToListAsync(ct);
    }

    public async Task<QuarantinedAttachmentDocument?> GetByQuarantinePathAsync(string quarantinePath, CancellationToken ct = default)
        => await _collection.Find(d => d.QuarantinePath == quarantinePath).FirstOrDefaultAsync(ct);

    public Task SetCleanPathAsync(string quarantinePath, string cleanPath, CancellationToken ct = default)
    {
        var update = Builders<QuarantinedAttachmentDocument>.Update.Set(d => d.CleanPath, cleanPath);
        return _collection.UpdateOneAsync(d => d.QuarantinePath == quarantinePath, update, cancellationToken: ct);
    }
}
