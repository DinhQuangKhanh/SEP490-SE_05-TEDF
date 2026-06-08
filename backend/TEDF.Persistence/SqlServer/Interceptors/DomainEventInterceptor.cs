using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Persistence.SqlServer.Interceptors
{
    /// <summary>
    /// Interceptor for dispatching domain events after save using MediatR.
    /// </summary>
    public class DomainEventInterceptor : SaveChangesInterceptor
    {
        private readonly IPublisher _publisher;

        public DomainEventInterceptor(IPublisher publisher)
        {
            _publisher = publisher;
        }

        /// <remarks>
        /// Domain events are only dispatched in the async path to avoid sync-over-async deadlocks.
        /// All EF Core SaveChanges calls in this project use the async overload via UnitOfWork.
        /// </remarks>
        public override int SavedChanges(
            SaveChangesCompletedEventData eventData,
            int result)
        {

            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken = default)
        {
            if (context is null) return;

            // Collect aggregates with pending events — snapshot the events before clearing
            var aggregateRoots = context.ChangeTracker
                .Entries()
                .Where(e => e.Entity is IHasDomainEvents)
                .Select(e => (IHasDomainEvents)e.Entity)
                .Where(ar => ar.DomainEvents.Count > 0)
                .ToList();

            // Snapshot all events, then clear immediately to prevent re-dispatch
            var domainEvents = aggregateRoots.SelectMany(ar => ar.DomainEvents)
                .ToList();

            foreach (var ar in aggregateRoots)
                ar.ClearDomainEvents();

            // Publish each event — handlers may trigger additional SaveChanges
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
