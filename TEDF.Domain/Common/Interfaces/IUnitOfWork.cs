
namespace TEDF.Domain.Common.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Saves all changes made in the current unit of work.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
