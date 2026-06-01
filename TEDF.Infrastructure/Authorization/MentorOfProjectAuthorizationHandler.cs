using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Infrastructure.Authorization.Requirements;
using AppClaimTypes = TEDF.Application.Common.AppClaimTypes;

namespace TEDF.Infrastructure.Authorization
{
    /// <summary>
    /// Handles mentor of project authorization.
    /// </summary>
    public class MentorOfProjectAuthorizationHandler : AuthorizationHandler<MentorOfProjectRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProjectRepository _projectRepository;

        public MentorOfProjectAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            IProjectRepository projectRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _projectRepository = projectRepository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MentorOfProjectRequirement requirement)
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

            var project = await _projectRepository.GetWithMentorsAsync(projectId.Value);
            if (project is null) return;

            if (project.Mentors.Any(m => m.MentorId == userId && m.IsActive))
            {
                context.Succeed(requirement);
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
