using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Specifications;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.Common
{
    /// <summary>
    /// Base repository implementation with common CRUD operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    public abstract class BaseRepository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : class, IIdentifiable<TId>
        where TId : notnull
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        protected BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public virtual void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        /// <remarks>
        /// Uses AnyAsync instead of FindAsync to avoid loading the entire entity into memory.
        /// </remarks>
        public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
        }

        /// <summary>
        /// Applies a specification to the query.
        /// </summary>
        protected IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
        {
            return SpecificationEvaluator<TEntity>.GetQuery(_dbSet.AsQueryable(), spec);
        }

        /// <summary>
        /// Gets entities matching the specification.
        /// </summary>
        protected async Task<IEnumerable<TEntity>> ListAsync(
            ISpecification<TEntity> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a single entity matching the specification.
        /// </summary>
        protected async Task<TEntity?> FirstOrDefaultAsync(
            ISpecification<TEntity> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Counts entities matching the specification.
        /// </summary>
        protected async Task<int> CountAsync(
            ISpecification<TEntity> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec).CountAsync(cancellationToken);
        }

        /// <summary>
        /// True when any entity matches the specification (short-circuits — cheaper than a count).
        /// </summary>
        protected async Task<bool> AnyAsync(
            ISpecification<TEntity> spec,
            CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(spec).AnyAsync(cancellationToken);
        }
    }

}
