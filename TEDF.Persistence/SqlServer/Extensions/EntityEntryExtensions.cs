using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Persistence.SqlServer.Extensions
{
    /// <summary>
    /// Extension methods for EntityEntry to help with entity tracking and manipulation.
    /// </summary>
    public static class EntityEntryExtensions
    {
        /// <summary>
        /// Checks if the entity has been modified.
        /// </summary>
        public static bool HasChanged(this EntityEntry entry)
        {
            return entry.State == EntityState.Modified ||
                   entry.State == EntityState.Added ||
                   entry.State == EntityState.Deleted;
        }

        /// <summary>
        /// Checks if a specific property has been modified.
        /// </summary>
        public static bool HasPropertyChanged(this EntityEntry entry, string propertyName)
        {
            if (entry.State == EntityState.Added) return true;
            if (entry.State == EntityState.Deleted) return false;

            var property = entry.Property(propertyName);
            return property.IsModified;
        }

        /// <summary>
        /// Gets the original value of a property.
        /// </summary>
        public static T? GetOriginalValue<T>(this EntityEntry entry, string propertyName)
        {
            return (T?)entry.Property(propertyName).OriginalValue;
        }

        /// <summary>
        /// Gets the current value of a property.
        /// </summary>
        public static T? GetCurrentValue<T>(this EntityEntry entry, string propertyName)
        {
            return (T?)entry.Property(propertyName).CurrentValue;
        }

        /// <summary>
        /// Marks all properties except the specified ones as not modified.
        /// </summary>
        public static void MarkPropertiesAsUnmodifiedExcept(this EntityEntry entry, params string[] propertyNames)
        {
            var propertySet = new HashSet<string>(propertyNames, StringComparer.OrdinalIgnoreCase);

            foreach (var property in entry.Properties)
            {
                if (!propertySet.Contains(property.Metadata.Name))
                {
                    property.IsModified = false;
                }
            }
        }

        /// <summary>
        /// Marks specific properties as modified.
        /// </summary>
        public static void MarkPropertiesAsModified(this EntityEntry entry, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                entry.Property(propertyName).IsModified = true;
            }
        }

        /// <summary>
        /// Gets the aggregate root from an entity entry if applicable.
        /// </summary>
        public static AggregateRoot<TId>? GetAggregateRoot<TId>(this EntityEntry entry) where TId : notnull
        {
            return entry.Entity as AggregateRoot<TId>;
        }

        /// <summary>
        /// Checks if the entity is an aggregate root.
        /// </summary>
        public static bool IsAggregateRoot(this EntityEntry entry)
        {
            var entityType = entry.Entity.GetType();
            return entityType.BaseType != null &&
                   entityType.BaseType.IsGenericType &&
                   entityType.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>);
        }

        /// <summary>
        /// Gets all domain events from aggregate roots in the change tracker.
        /// </summary>
        public static IEnumerable<object> GetDomainEvents(this ChangeTracker changeTracker)
        {
            var aggregateRoots = changeTracker.Entries()
                .Where(e => e.Entity is IHasDomainEvents)
                .Select(e => e.Entity as IHasDomainEvents)
                .Where(ar => ar != null && ar.DomainEvents.Any())
                .ToList();

            return aggregateRoots.SelectMany(ar => ar!.DomainEvents);
        }

        /// <summary>
        /// Clears all domain events from aggregate roots in the change tracker.
        /// </summary>
        public static void ClearDomainEvents(this ChangeTracker changeTracker)
        {
            var aggregateRoots = changeTracker.Entries()
                .Where(e => e.Entity is IHasDomainEvents)
                .Select(e => e.Entity as IHasDomainEvents)
                .Where(ar => ar != null)
                .ToList();

            foreach (var ar in aggregateRoots)
            {
                ar!.ClearDomainEvents();
            }
        }

        /// <summary>
        /// Gets modified properties with their original and current values.
        /// </summary>
        public static IEnumerable<(string PropertyName, object? OriginalValue, object? CurrentValue)> GetModifiedProperties(this EntityEntry entry)
        {
            if (entry.State != EntityState.Modified)
                yield break;

            foreach (var property in entry.Properties.Where(p => p.IsModified))
            {
                yield return (property.Metadata.Name, property.OriginalValue, property.CurrentValue);
            }
        }

        /// <summary>
        /// Sets the entity state to detached.
        /// </summary>
        public static void Detach(this EntityEntry entry)
        {
            entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Checks if the entity is soft deletable and has been soft deleted.
        /// </summary>
        public static bool IsSoftDeleted(this EntityEntry entry)
        {
            if (entry.Entity is ISoftDeletable softDeletable)
            {
                return softDeletable.IsDeleted;
            }
            return false;
        }
    }
}
