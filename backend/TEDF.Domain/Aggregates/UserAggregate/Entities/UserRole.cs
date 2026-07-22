using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Entities;

namespace TEDF.Domain.Aggregates.UserAggregate.Entities
{
    /// <summary>
    /// Entity representing a role assigned to a user.
    /// </summary>
    public class UserRole : Entity<int>
    {
        public Guid UserId { get; private set; }
        public int RoleId { get; private set; }
        public Role? Role { get; private set; }

        /// <summary>Computed from navigation property. Requires Role to be eagerly loaded.</summary>
        public string RoleName => Role?.Name ?? string.Empty;

        public DateTime AssignedAt { get; private set; }
        public Guid? AssignedBy { get; private set; }
        public bool IsActive { get; private set; } = true;

        private UserRole() { }

        public static UserRole Create(Guid userId, int roleId, Guid? assignedBy = null)
        {
            return new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
                IsActive = true
            };
        }

        public void Deactivate() => IsActive = false;

        public void Reactivate() => IsActive = true;
    }
}
