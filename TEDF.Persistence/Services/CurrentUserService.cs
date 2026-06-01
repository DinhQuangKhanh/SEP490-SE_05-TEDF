using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Persistence.Services
{
    /// <summary>
    /// Implementation of ICurrentUserService using HttpContext.
    /// Provides access to current user information from the HTTP context.
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Guid? UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(AppClaimTypes.DbUserId);
                return string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
            }
        }

        /// <inheritdoc />
        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

        /// <inheritdoc />
        public string? FullName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        /// <inheritdoc />
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <inheritdoc />
        public IEnumerable<string> Roles
        {
            get
            {
                var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role);
                return roles?.Select(r => r.Value) ?? [];
            }
        }

        /// <inheritdoc />
        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }
    }
}
