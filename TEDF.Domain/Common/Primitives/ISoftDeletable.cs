

namespace TEDF.Domain.Common.Primitives
{
    public interface ISoftDeletable
    {
        /// <summary>
        /// Gets a value indicating whether the entity is deleted.
        /// </summary>
        bool IsDeleted { get; }

        /// <summary>
        /// Gets the date and time when the entity was deleted.
        /// </summary>
        DateTime? DeletedAt { get; }

        /// <summary>
        /// Gets the identifier of the user who deleted the entity.
        /// </summary>
        Guid? DeletedBy { get; }

        /// <summary>
        /// Marks the entity as deleted.
        /// </summary>
        /// <param name="deletedBy">The identifier of the user who deleted the entity.</param>
        void Delete(Guid? deletedBy = null);

        /// <summary>
        /// Restores a soft-deleted entity.
        /// </summary>
        void Restore();
    }
}
