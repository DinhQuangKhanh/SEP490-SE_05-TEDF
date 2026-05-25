
namespace TEDF.Domain.Common.Exceptions
{
    public class ConcurrencyException : DomainException
    {
        /// <summary>
        /// Gets the type name of the entity involved in the conflict.
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the identifier of the entity involved in the conflict.
        /// </summary>
        public object EntityId { get; }

        /// <summary>
        /// Initializes a new instance of the ConcurrencyException class.
        /// </summary>
        /// <param name="entityType">The type name of the entity.</param>
        /// <param name="entityId">The identifier of the entity.</param>
        public ConcurrencyException(string entityType, object entityId)
            : base(
                $"A concurrency conflict occurred while updating {entityType} with id '{entityId}'. " +
                "The entity has been modified by another user.",
                "CONCURRENCY_CONFLICT")
        {
            EntityType = entityType;
            EntityId = entityId;
        }

        /// <summary>
        /// Creates a ConcurrencyException for a specific entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entityId">The identifier of the entity.</param>
        /// <returns>A new ConcurrencyException instance.</returns>
        public static ConcurrencyException For<TEntity>(object entityId)
        {
            return new ConcurrencyException(typeof(TEntity).Name, entityId);
        }
    }
}
