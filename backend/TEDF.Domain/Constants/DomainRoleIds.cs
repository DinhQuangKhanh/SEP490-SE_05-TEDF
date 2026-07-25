namespace TEDF.Domain.Constants
{
    public static class DomainRoleIds
    {
        public const int Admin = 1;
        public const int Mentor = 2;
        public const int Student = 3;
        public const int Evaluator = 4;
        public const int DepartmentHead = 5;

        public static int FromName(string roleName) => roleName switch
        {
            DomainRoleNames.Admin => Admin,
            DomainRoleNames.Mentor => Mentor,
            DomainRoleNames.Student => Student,
            DomainRoleNames.Evaluator => Evaluator,
            DomainRoleNames.DepartmentHead => DepartmentHead,
            _ => throw new ArgumentException($"Unknown role name: {roleName}", nameof(roleName))
        };
    }
}
