namespace TEDF.Infrastructure.Caching
{
    public static class CacheKeys
    {
        // Semester
        public static string ActiveSemester => "semester:active";
        public static string SemesterById(int id) => $"semester:{id}";

        // Project
        public static string ProjectById(Guid id) => $"project:{id}";

        // Group
        public static string GroupById(Guid id) => $"group:{id}";

        // User
        public static string UserPermissions(Guid userId) => $"user:{userId}:permissions";

        // System
        public static string SystemConfig(string key) => $"config:{key}";
        public static string DepartmentList => "departments:all";
        public static string MajorsByDepartment(int departmentId) => $"majors:dept:{departmentId}";

        // Stats
        public static string ProjectStats(int semesterId) => $"stats:project:{semesterId}";
        public static string EvaluationStats(int semesterId) => $"stats:evaluation:{semesterId}";

        // User management
        public const string UserListPrefix = "users:list:";

        // Evaluator
        public const string EvaluatorPrefix = "evaluator:";
        public static string EvaluatorFilterOptions => "evaluator:filter-options";
        public static string EvaluatorDashboard(Guid evaluatorId) => $"evaluator:{evaluatorId}:dashboard";
        public static string EvaluatorProjectsPrefix(Guid evaluatorId) => $"evaluator:{evaluatorId}:projects";
        public static string EvaluatorProjects(Guid evaluatorId, int page, int pageSize, string? search, int? semesterId, int? majorId, string? result)
            => $"evaluator:{evaluatorId}:projects:{page}:{pageSize}:{search ?? "_"}:{semesterId?.ToString() ?? "_"}:{majorId?.ToString() ?? "_"}:{result ?? "_"}";
        public static string EvaluatorHistoryPrefix(Guid evaluatorId) => $"evaluator:{evaluatorId}:history";
        public static string EvaluatorHistory(Guid evaluatorId, int page, int pageSize, string? search, string? result, string? dateRange)
            => $"evaluator:{evaluatorId}:history:{page}:{pageSize}:{search ?? "_"}:{result ?? "_"}:{dateRange ?? "_"}";
    }
}
