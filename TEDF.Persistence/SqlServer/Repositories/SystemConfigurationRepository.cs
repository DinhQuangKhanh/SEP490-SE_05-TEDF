using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository for SystemConfiguration entity.
    /// </summary>
    public class SystemConfigurationRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<SystemConfiguration> _dbSet;

        public SystemConfigurationRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<SystemConfiguration>();
        }

        public async Task<SystemConfiguration?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public async Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        }

        public async Task<IEnumerable<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.OrderBy(c => c.Category).ThenBy(c => c.Key).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SystemConfiguration>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(c => c.Category == category)
                .OrderBy(c => c.Key)
                .ToListAsync(cancellationToken);
        }

        public async Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var config = await GetByKeyAsync(key, cancellationToken);
            return config == null ? default : config.GetValue<T>();
        }

        public async Task AddAsync(SystemConfiguration config, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(config, cancellationToken);
        }

        public void Update(SystemConfiguration config)
        {
            _dbSet.Update(config);
        }

        public void Remove(SystemConfiguration config)
        {
            _dbSet.Remove(config);
        }

        public async Task<bool> ExistsKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(c => c.Key == key, cancellationToken);
        }

        public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
        {
            var maxId = await _dbSet.MaxAsync(c => (int?)c.Id, cancellationToken);
            return (maxId ?? 0) + 1;
        }
    }
}
