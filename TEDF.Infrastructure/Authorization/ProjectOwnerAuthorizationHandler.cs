using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Infrastructure.Authorization.Requirements;
using AppClaimTypes = TEDF.Application.Common.AppClaimTypes;

namespace TEDF.Infrastructure.Authorization
{
    /// <summary>
    /// Handles project owner authorization.
    /// </summary>
    public class ProjectOwnerAuthorizationHandler : AuthorizationHandler<ProjectOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProjectRepository _projectRepository;
        private readonly IGroupRepository _groupRepository;

        public ProjectOwnerAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _projectRepository = projectRepository;
            _groupRepository = groupRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ProjectOwnerRequirement requirement)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userId = GetUserId(context.User);
            if (userId is null) return;

            var projectId = GetProjectIdFromRoute();
            if (projectId is null) return;

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            if (project is null) return;

            // Check if user is the leader of the group that owns this project
            if (project.GroupId.HasValue)
            {
                var group = await _groupRepository.GetWithMembersAsync(project.GroupId.Value);
                if (group?.LeaderId == userId)
                {
                    context.Succeed(requirement);
                }
            }
        }

        private Guid? GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(AppClaimTypes.DbUserId)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private Guid? GetProjectIdFromRoute()
        {
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var projectIdStr = routeData?.Values["projectId"]?.ToString() ??
                              routeData?.Values["id"]?.ToString();
            return Guid.TryParse(projectIdStr, out var projectId) ? projectId : null;
        }
    }
}
