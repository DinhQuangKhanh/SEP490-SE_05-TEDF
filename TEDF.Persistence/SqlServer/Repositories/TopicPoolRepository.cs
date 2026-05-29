using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Enums.TopicPool;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories;

/// <summary>
/// Repository implementation for TopicPool aggregate.
/// </summary>
public class TopicPoolRepository : BaseRepository<TopicPool, Guid>, ITopicPoolRepository
{
    public TopicPoolRepository(AppDbContext context) : base(context) { }

    public async Task<TopicPool?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(tp => tp.Code == code, cancellationToken);
    }

    public async Task<TopicPool?> GetByMajorIdAsync(int majorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(tp => tp.MajorId == majorId, cancellationToken);
    }

    public async Task<IEnumerable<TopicPool>> GetActivePoolsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tp => tp.Status == TopicPoolStatus.Active)
            .OrderBy(tp => tp.MajorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(tp => tp.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsForMajorAsync(int majorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(tp => tp.MajorId == majorId, cancellationToken);
    }
}
