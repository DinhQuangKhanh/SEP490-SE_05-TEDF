using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Configuration helpers for Azure Application Insights.
    /// </summary>
    public static class ApplicationInsightsConfiguration
    {
        /// <summary>
        /// Adds Application Insights telemetry services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddApplicationInsightsInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var settings = configuration.GetSection("ApplicationInsights").Get<ApplicationInsightsSettings>()
                ?? new ApplicationInsightsSettings();

            if (!settings.Enabled || string.IsNullOrEmpty(settings.ConnectionString))
            {
                return services;
            }

            // Configure Application Insights
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = settings.ConnectionString;
                options.EnableAdaptiveSampling = settings.EnableAdaptiveSampling;
                options.EnableQuickPulseMetricStream = settings.EnableLiveMetrics;
                options.EnableDependencyTrackingTelemetryModule = settings.EnableDependencyTracking;
                options.EnableRequestTrackingTelemetryModule = settings.EnableRequestTracking;
            });

            // Configure telemetry initializers
            services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();

            // Configure dependency tracking
            if (settings.EnableDependencyTracking)
            {
                services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
                {
                    module.EnableSqlCommandTextInstrumentation = settings.EnableSqlCommandTextInstrumentation;
                });
            }

            // Add custom telemetry processor for filtering
            if (settings.FilterHealthCheckRequests)
            {
                services.AddApplicationInsightsTelemetryProcessor<HealthCheckTelemetryFilter>();
            }

            return services;
        }

        /// <summary>
        /// Uses Application Insights in the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseApplicationInsightsInfrastructure(this IApplicationBuilder app)
        {
            // Application Insights middleware is automatically added
            return app;
        }
    }
}
