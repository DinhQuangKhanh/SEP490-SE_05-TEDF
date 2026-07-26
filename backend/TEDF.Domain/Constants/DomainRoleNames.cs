namespace TEDF.Domain.Constants
{
    /// <summary>
    /// Predefined role names in the domain.
    /// These are the canonical role names used throughout the system.
    /// </summary>
    public static class DomainRoleNames
    {
        public const string Admin = "Admin";
        public const string Mentor = "Mentor";
        public const string Student = "Student";
        public const string Evaluator = "Evaluator";
        public const string DepartmentHead = "DepartmentHead";

        /// <summary>
        /// Reverse of <see cref="DomainRoleIds.FromName"/>. Returns null for an id outside the five
        /// seeded roles, so callers can fall back to the Roles table instead of guessing.
        /// </summary>
        public static string? FromId(int roleId) => roleId switch
        {
            DomainRoleIds.Admin => Admin,
            DomainRoleIds.Mentor => Mentor,
            DomainRoleIds.Student => Student,
            DomainRoleIds.Evaluator => Evaluator,
            DomainRoleIds.DepartmentHead => DepartmentHead,
            _ => null
        };
    }
}
