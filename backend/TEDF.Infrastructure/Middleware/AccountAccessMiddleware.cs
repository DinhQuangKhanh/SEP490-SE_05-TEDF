using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Middleware
{
    /// <summary>
    /// Blocks API and SignalR access for accounts that are locked/inactive, or for student-only accounts
    /// not on any active/upcoming eligible list. Must run AFTER authentication. The /api/auth/* endpoints
    /// are allowlisted so an ineligible student can still call /api/auth/session and learn why they're
    /// blocked. The per-user decision is cached briefly to avoid a DB hit on every request.
    /// </summary>
    public class AccountAccessMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static readonly string[] Allowlist =
        [
            "/api/auth",
            "/api/settings/public",
            "/health",
            "/swagger",
            "/hangfire"
        ];

        /// <summary>Path prefixes the gate applies to. SignalR is included: a blocked account must not
        /// keep a live chat/notification connection just because the hubs are not under /api.</summary>
        private static readonly string[] GatedPrefixes = ["/api", "/hubs"];

        /// <summary>
        /// Decision kinds that come from the account status itself. Admins bypass the eligibility rules
        /// (semester roster), but never these — a locked or disabled admin account stays blocked.
        /// </summary>
        private static readonly string[] StatusBlockedKinds = ["locked", "inactive"];

        public AccountAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            var isGated = GatedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            var isAllowlisted = Allowlist.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!isGated || isAllowlisted || context.User?.Identity?.IsAuthenticated != true)
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

            // Admins skip the eligibility rules but not the status ones, so the decision is evaluated
            // for them too and only then filtered.
            var bypassesEligibility = context.User.IsInRole("Admin");
            if (decision is { Allowed: false }
                && (!bypassesEligibility || StatusBlockedKinds.Contains(decision.Kind)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var payload = ApiResponse.Fail(decision.Reason ?? "Bạn không có quyền truy cập hệ thống.");
                var json = JsonSerializer.Serialize(payload, JsonOptions);
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
