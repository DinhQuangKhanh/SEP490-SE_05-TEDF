using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.UserAggregate.Entities
{
    /// <summary>
    /// Entity representing a role assigned to a user.
    /// </summary>
    public class UserRole : Entity<int>
    {
        public Guid UserId { get; private set; }
        public string RoleName { get; private set; } = string.Empty;
        public DateTime AssignedAt { get; private set; }
        public Guid? AssignedBy { get; private set; }
        public bool IsActive { get; private set; } = true;

        private UserRole() { }

        /// <summary>
        /// Creates a new UserRole entity.
        /// </summary>
        public static UserRole Create(Guid userId, string roleName, Guid? assignedBy = null)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name cannot be empty.", nameof(roleName));

            return new UserRole
            {
                UserId = userId,
                RoleName = roleName,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
                IsActive = true
            };
        }

        /// <summary>
        /// Deactivates this role assignment.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Reactivates this role assignment.
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
        }
    }
}
