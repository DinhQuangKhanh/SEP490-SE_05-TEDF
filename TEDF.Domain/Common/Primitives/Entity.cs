
namespace TEDF.Domain.Common.Primitives
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>, IIdentifiable<TId>
    where TId : notnull
    {
        public TId Id { get; protected init; } = default!;

        protected Entity(TId id)
        {
            Id = id;
        }

        protected Entity() { }

        public bool Equals(Entity<TId>? other)
        {
            return other is not null && Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is Entity<TId> entity && Equals(entity);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
            => Equals(left, right);

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
            => !Equals(left, right);
    }
}
