using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository for Major entity.
    /// </summary>
    public class MajorRepository : IMajorReadRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Major> _dbSet;

        public MajorRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<Major>();
        }

        public async Task<Major?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public async Task<Major?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(m => m.Code == code.ToUpperInvariant(), cancellationToken);
        }

        public async Task<IEnumerable<Major>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.OrderBy(m => m.Name).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Major>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Major>> GetByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(m => m.DepartmentId == departmentId && m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Major major, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(major, cancellationToken);
        }

        public void Update(Major major)
        {
            _dbSet.Update(major);
        }

        public void Remove(Major major)
        {
            _dbSet.Remove(major);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(m => m.Code == code.ToUpperInvariant(), cancellationToken);
        }

        public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
        {
            var maxId = await _dbSet.MaxAsync(m => (int?)m.Id, cancellationToken);
            return (maxId ?? 0) + 1;
        }
    }
}
