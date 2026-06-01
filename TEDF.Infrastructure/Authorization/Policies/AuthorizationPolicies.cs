using Microsoft.Extensions.DependencyInjection;
using TEDF.Infrastructure.Authorization.Requirements;

namespace TEDF.Infrastructure.Authorization.Policies
{
    /// <summary>
    /// Extension methods for configuring authorization policies.
    /// </summary>
    public static class AuthorizationPolicies
    {
        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Role-based policies
                options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy(PolicyNames.RequireMentor, policy =>
                    policy.RequireRole("Admin", "Mentor"));

                options.AddPolicy(PolicyNames.RequireStudent, policy =>
                    policy.RequireRole("Admin", "Student"));

                options.AddPolicy(PolicyNames.RequireEvaluator, policy =>
                    policy.RequireRole("Admin", "Evaluator"));

                options.AddPolicy(PolicyNames.RequireDepartmentHead, policy =>
                    policy.RequireRole("Admin", "DepartmentHead"));

                // Permission-based policies
                options.AddPolicy(PolicyNames.CanManageProjects, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Permissions.ProjectUpdate)));

                options.AddPolicy(PolicyNames.CanEvaluateProjects, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Permissions.EvaluationComplete)));

                options.AddPolicy(PolicyNames.CanManageGroups, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Permissions.GroupUpdate)));

                options.AddPolicy(PolicyNames.CanManageSemesters, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Permissions.SemesterManage)));

                options.AddPolicy(PolicyNames.CanManageTopics, policy =>
                    policy.Requirements.Add(new PermissionRequirement(Permissions.TopicUpdate)));

                // Resource-based policies
                options.AddPolicy(PolicyNames.ProjectOwner, policy =>
                    policy.Requirements.Add(new ProjectOwnerRequirement()));

                options.AddPolicy(PolicyNames.GroupMember, policy =>
                    policy.Requirements.Add(new GroupMemberRequirement()));

                options.AddPolicy(PolicyNames.GroupLeader, policy =>
                    policy.Requirements.Add(new GroupLeaderRequirement()));

                options.AddPolicy(PolicyNames.MentorOfProject, policy =>
                    policy.Requirements.Add(new MentorOfProjectRequirement()));

                options.AddPolicy(PolicyNames.DepartmentHeadOfDepartment, policy =>
                    policy.Requirements.Add(new DepartmentHeadOfDepartmentRequirement()));
            });
        }
    }
}
