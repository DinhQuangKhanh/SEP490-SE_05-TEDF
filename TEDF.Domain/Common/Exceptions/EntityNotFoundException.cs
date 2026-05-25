
namespace TEDF.Domain.Common.Exceptions
{
    public class EntityNotFoundException : DomainException
    {
        /// <summary>
        /// Gets the type name of the entity that was not found.
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Gets the identifier of the entity that was not found.
        /// </summary>
        public object EntityId { get; }

        /// <summary>
        /// Initializes a new instance of the EntityNotFoundException class.
        /// </summary>
        /// <param name="entityType">The type name of the entity.</param>
        /// <param name="entityId">The identifier of the entity.</param>
        public EntityNotFoundException(string entityType, object entityId)
            : base($"{entityType} with id '{entityId}' was not found.", "ENTITY_NOT_FOUND")
        {
            EntityType = entityType;
            EntityId = entityId;
        }

        /// <summary>
        /// Initializes a new instance of the EntityNotFoundException class.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="entityId">The identifier of the entity.</param>
        /// <returns>A new EntityNotFoundException instance.</returns>
        public static EntityNotFoundException For<TEntity>(object entityId)
        {
            return new EntityNotFoundException(typeof(TEntity).Name, entityId);
        }
    }
}
