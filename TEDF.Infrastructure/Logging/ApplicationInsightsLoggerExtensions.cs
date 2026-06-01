using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Extension methods for Application Insights logging.
    /// Requires <see cref="TelemetryClient"/> to be registered in DI (via AddApplicationInsightsTelemetry).
    /// </summary>
    public static class ApplicationInsightsLoggerExtensions
    {
        /// <summary>
        /// Tracks a custom event in Application Insights.
        /// </summary>
        public static void TrackEvent(this ILogger logger, IServiceProvider serviceProvider, string eventName, IDictionary<string, string>? properties = null)
        {
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            telemetryClient?.TrackEvent(eventName, properties);

            logger.LogInformation("Custom event tracked: {EventName}", eventName);
        }

        /// <summary>
        /// Tracks a custom metric in Application Insights.
        /// </summary>
        public static void TrackMetric(this ILogger logger, IServiceProvider serviceProvider, string metricName, double value, IDictionary<string, string>? properties = null)
        {
            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            telemetryClient?.TrackMetric(metricName, value, properties);

            logger.LogDebug("Custom metric tracked: {MetricName} = {Value}", metricName, value);
        }
    }
}
