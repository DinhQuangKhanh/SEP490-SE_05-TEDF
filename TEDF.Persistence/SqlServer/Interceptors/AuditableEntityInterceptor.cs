using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Persistence.SqlServer.Interceptors
{
    /// <summary>
    /// Interceptor for automatically setting audit fields on entities.
    /// </summary>
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditableEntityInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context is null) return;

            var userId = _currentUserService.UserId;

            // DRY: one generic helper handles both Guid and int keyed auditable entities
            SetAuditFields(context.ChangeTracker.Entries<AuditableEntity<Guid>>(), userId);
            SetAuditFields(context.ChangeTracker.Entries<AuditableEntity<int>>(), userId);
        }

        /// <summary>
        /// Applies Created/Updated audit fields based on entity state.
        /// </summary>
        private static void SetAuditFields<TId>(
            IEnumerable<EntityEntry<AuditableEntity<TId>>> entries, Guid? userId)
            where TId : notnull
        {
            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.SetCreated(userId);
                        break;
                    case EntityState.Modified:
                        entry.Entity.SetUpdated(userId);
                        break;
                }
            }
        }
    }
}
