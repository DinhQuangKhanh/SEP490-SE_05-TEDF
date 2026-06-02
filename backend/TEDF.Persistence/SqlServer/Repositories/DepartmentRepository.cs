using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository for Department entity.
    /// </summary>
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Department> _dbSet;

        public DepartmentRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<Department>();
        }

        public async Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public async Task<Department?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FirstOrDefaultAsync(d => d.Code == code.ToUpperInvariant(), cancellationToken);
        }

        public async Task<IEnumerable<Department>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.OrderBy(d => d.Name).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Department>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Department department, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(department, cancellationToken);
        }

        public void Update(Department department)
        {
            _dbSet.Update(department);
        }

        public void Remove(Department department)
        {
            _dbSet.Remove(department);
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(d => d.Code == code.ToUpperInvariant(), cancellationToken);
        }

        public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
        {
            var maxId = await _dbSet.MaxAsync(d => (int?)d.Id, cancellationToken);
            return (maxId ?? 0) + 1;
        }

        public async Task<bool> IsMajorInDepartmentAsync(int majorId, int departmentId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Major>()
                .AnyAsync(m => m.Id == majorId && m.DepartmentId == departmentId, cancellationToken);
        }
    }
}
