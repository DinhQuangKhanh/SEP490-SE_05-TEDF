
namespace TEDF.Domain.Common.Primitives
{
    public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
    {
        /// <summary>
        /// Gets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedAt { get; protected set; }

        /// <summary>
        /// Gets the identifier of the user who created the entity.
        /// </summary>
        public Guid? CreatedBy { get; protected set; }

        /// <summary>
        /// Gets the date and time when the entity was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; protected set; }

        /// <summary>
        /// Gets the identifier of the user who last updated the entity.
        /// </summary>
        public Guid? UpdatedBy { get; protected set; }

        /// <summary>
        /// Initializes a new instance with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        protected AuditableEntity(TId id) : base(id)
        {
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance. Required for EF Core.
        /// </summary>
        protected AuditableEntity() { }

        /// <summary>
        /// Sets the creation audit information.
        /// </summary>
        /// <param name="userId">The identifier of the user who created the entity.</param>
        public void SetCreated(Guid? userId = null)
        {
            CreatedAt = DateTime.UtcNow;
            CreatedBy = userId;
        }

        /// <summary>
        /// Sets the update audit information.
        /// </summary>
        /// <param name="userId">The identifier of the user who updated the entity.</param>
        public void SetUpdated(Guid? userId = null)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = userId;
        }
    }
}
