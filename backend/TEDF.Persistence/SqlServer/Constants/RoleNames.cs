namespace TEDF.Persistence.SqlServer.Constants
{
    /// <summary>
    /// Predefined role names in the system.
    /// </summary>
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Mentor = "Mentor";
        public const string Student = "Student";
        public const string Evaluator = "Evaluator";
        public const string DepartmentHead = "DepartmentHead";

        public static readonly string[] All = [Admin, Mentor, Student, Evaluator, DepartmentHead];
    }
}