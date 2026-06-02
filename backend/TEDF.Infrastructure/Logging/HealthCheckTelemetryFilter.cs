using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Telemetry processor to filter out health check requests.
    /// </summary>
    internal class HealthCheckTelemetryFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        /// <summary>
        /// Static readonly array avoids allocating a new array on every request.
        /// </summary>
        private static readonly string[] HealthCheckPaths =
        [
            "/health",
            "/healthz",
            "/ready",
            "/live",
            "/ping",
            "/api/health"
        ];

        public HealthCheckTelemetryFilter(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request && IsHealthCheckRequest(request.Url?.AbsolutePath))
                return;

            if (item is DependencyTelemetry dependency && IsHealthCheckRequest(dependency.Data))
                return;

            _next.Process(item);
        }

        private static bool IsHealthCheckRequest(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return HealthCheckPaths.Any(hcp => path.Contains(hcp, StringComparison.OrdinalIgnoreCase));
        }
    }
}
