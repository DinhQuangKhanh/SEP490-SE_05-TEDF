using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Middleware
{
    /// <summary>
    /// When the <c>MaintenanceMode</c> setting is on, blocks non-admin API calls with HTTP 503.
    /// Must run AFTER authentication so the caller's roles are known. Auth and the public-settings /
    /// health endpoints stay allowlisted so the SPA can still render the maintenance page and admins
    /// can log in.
    /// </summary>
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;

        // Prefixes that remain reachable during maintenance.
        private static readonly string[] Allowlist =
        [
            "/api/auth",
            "/api/settings/public",
            "/health",
            "/swagger",
            "/hangfire"
        ];

        public MaintenanceModeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Only guard the API; ignore non-API and allowlisted paths.
            var isApi = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);
            var isAllowlisted = Allowlist.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            if (!isApi || isAllowlisted)
            {
                await _next(context);
                return;
            }

            // Admins always have full access.
            if (context.User?.IsInRole("Admin") == true)
            {
                await _next(context);
                return;
            }

            var settings = context.RequestServices.GetService<ISystemSettingsService>();
            if (settings is not null
                && await settings.GetBoolAsync(SettingKeys.MaintenanceMode, false, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                var payload = ApiResponse.Fail("Hệ thống đang bảo trì. Vui lòng quay lại sau.");
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

    public static class MaintenanceModeMiddlewareExtensions
    {
        /// <summary>Registers <see cref="MaintenanceModeMiddleware"/>. Call after UseAuthorization().</summary>
        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
            => app.UseMiddleware<MaintenanceModeMiddleware>();
    }
}
