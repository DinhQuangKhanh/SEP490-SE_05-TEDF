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
    /// Handles group leader authorization.
    /// </summary>
    public class GroupLeaderAuthorizationHandler : AuthorizationHandler<GroupLeaderRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGroupRepository _groupRepository;
        private readonly IProjectRepository _projectRepository;

        public GroupLeaderAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            IGroupRepository groupRepository,
            IProjectRepository projectRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _groupRepository = groupRepository;
            _projectRepository = projectRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GroupLeaderRequirement requirement)
        {
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var userId = GetUserId(context.User);
            if (userId is null) return;

            var groupId = await ResolveGroupIdAsync();
            if (groupId is null) return;

            if (await _groupRepository.IsLeaderOfGroupAsync(userId.Value, groupId.Value))
            {
                context.Succeed(requirement);
            }
        }

        private Guid? GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(AppClaimTypes.DbUserId)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private Guid? GetGroupIdFromRoute()
        {
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var groupIdStr = routeData?.Values["groupId"]?.ToString() ??
                             routeData?.Values["id"]?.ToString();

            return Guid.TryParse(groupIdStr, out var groupId) ? groupId : null;
        }

        private Guid? GetProjectIdFromRoute()
        {
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var projectIdStr = routeData?.Values["projectId"]?.ToString();

            return Guid.TryParse(projectIdStr, out var projectId) ? projectId : null;
        }

        private async Task<Guid?> ResolveGroupIdAsync()
        {
            var groupId = GetGroupIdFromRoute();
            if (groupId.HasValue)
                return groupId;

            var projectId = GetProjectIdFromRoute();
            if (!projectId.HasValue)
                return null;

            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            return project?.GroupId;
        }
    }
}