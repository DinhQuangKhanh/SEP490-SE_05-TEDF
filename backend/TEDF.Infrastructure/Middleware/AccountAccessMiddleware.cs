using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Middleware
{
    /// <summary>
    /// Blocks API access for accounts that are locked/inactive, or for student-only accounts not on any
    /// active/upcoming eligible list. Must run AFTER authentication. The /api/auth/* endpoints are
    /// allowlisted so an ineligible student can still call /api/auth/session and learn why they're blocked.
    /// The per-user decision is cached briefly to avoid a DB hit on every request.
    /// </summary>
    public class AccountAccessMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly string[] Allowlist =
        [
            "/api/auth",
            "/api/settings/public",
            "/health",
            "/swagger",
            "/hangfire"
        ];

        public AccountAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            var isApi = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
            var isAllowlisted = Allowlist.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!isApi || isAllowlisted || context.User?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            // Admins always have full access.
            if (context.User.IsInRole("Admin"))
            {
                await _next(context);
                return;
            }

            var userIdValue = context.User.FindFirst(AppClaimTypes.DbUserId)?.Value;
            if (!Guid.TryParse(userIdValue, out var userId))
            {
                await _next(context);
                return;
            }

            var cache = context.RequestServices.GetRequiredService<ICacheService>();
            var accessControl = context.RequestServices.GetRequiredService<IAccessControlService>();

            var decision = await cache.GetOrSetAsync(
                $"access:{userId}",
                () => accessControl.EvaluateAsync(userId, context.RequestAborted),
                TimeSpan.FromSeconds(60),
                context.RequestAborted);

            if (decision is { Allowed: false })
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var payload = ApiResponse.Fail(decision.Reason ?? "Bạn không có quyền truy cập hệ thống.");
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(json);
                return;
            }

            await _next(context);
        }
    }

    public static class AccountAccessMiddlewareExtensions
    {
        /// <summary>Registers <see cref="AccountAccessMiddleware"/>. Call after UseAuthorization().</summary>
        public static IApplicationBuilder UseAccountAccessGate(this IApplicationBuilder app)
            => app.UseMiddleware<AccountAccessMiddleware>();
    }
}
