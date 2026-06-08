using Microsoft.AspNetCore.Builder;

namespace TEDF.Infrastructure.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionHandlingMiddleware>();

        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
            => app.UseMiddleware<RequestLoggingMiddleware>();

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();

        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
            => app.UseMiddleware<PerformanceMonitoringMiddleware>();
    }
}
