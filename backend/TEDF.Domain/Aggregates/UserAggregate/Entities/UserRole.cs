using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Constants;
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

        /// <summary>Navigation property — populated by EF Core through the backing field.</summary>
        public Role? Role { get; }

        /// <summary>
        /// Name of the assigned role. Resolved from <see cref="RoleId"/> against the seeded role
        /// constants, so it does not require the <see cref="Role"/> navigation to be loaded —
        /// nothing in the codebase eagerly loads it. Falls back to the navigation for any role
        /// added to the Roles table beyond the five seeded ones.
        /// </summary>
        public string RoleName => DomainRoleNames.FromId(RoleId) ?? Role?.Name ?? string.Empty;

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
