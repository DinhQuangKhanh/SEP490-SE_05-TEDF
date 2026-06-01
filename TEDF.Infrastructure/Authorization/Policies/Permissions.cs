
namespace TEDF.Infrastructure.Authorization.Policies
{
    /// <summary>
    /// Permission constants.
    /// </summary>
    public static class Permissions
    {
        // Project
        public const string ProjectCreate = "project.create";
        public const string ProjectRead = "project.read";
        public const string ProjectUpdate = "project.update";
        public const string ProjectDelete = "project.delete";
        public const string ProjectSubmit = "project.submit";
        public const string ProjectApprove = "project.approve";
        public const string ProjectReject = "project.reject";

        // Group
        public const string GroupCreate = "group.create";
        public const string GroupRead = "group.read";
        public const string GroupUpdate = "group.update";
        public const string GroupDelete = "group.delete";
        public const string GroupAddMember = "group.add_member";
        public const string GroupRemoveMember = "group.remove_member";

        // Evaluation
        public const string EvaluationRead = "evaluation.read";
        public const string EvaluationAssign = "evaluation.assign";
        public const string EvaluationComplete = "evaluation.complete";

        // Semester
        public const string SemesterManage = "semester.manage";

        // Topic Pool
        public const string TopicCreate = "topic.create";
        public const string TopicRead = "topic.read";
        public const string TopicUpdate = "topic.update";
        public const string TopicDelete = "topic.delete";

        // Reports
        public const string ReportGenerate = "report.generate";
        public const string ReportRead = "report.read";

        // System
        public const string SystemConfigManage = "system.config";
        public const string UserManage = "user.manage";
    }
}
