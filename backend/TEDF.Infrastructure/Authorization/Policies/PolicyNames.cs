
namespace TEDF.Infrastructure.Authorization.Policies
{
    /// <summary>
    /// Authorization policy names.
    /// </summary>
    public static class PolicyNames
    {
        public const string RequireAdmin = "RequireAdmin";
        public const string RequireMentor = "RequireMentor";
        public const string RequireStudent = "RequireStudent";
        public const string RequireEvaluator = "RequireEvaluator";
        public const string RequireDepartmentHead = "RequireDepartmentHead";

        public const string CanManageProjects = "CanManageProjects";
        public const string CanEvaluateProjects = "CanEvaluateProjects";
        public const string CanManageGroups = "CanManageGroups";
        public const string CanManageSemesters = "CanManageSemesters";
        public const string CanManageTopics = "CanManageTopics";

        public const string ProjectOwner = "ProjectOwner";
        public const string GroupMember = "GroupMember";
        public const string GroupLeader = "GroupLeader";
        public const string MentorOfProject = "MentorOfProject";
        public const string DepartmentHeadOfDepartment = "DepartmentHeadOfDepartment";
    }
}
