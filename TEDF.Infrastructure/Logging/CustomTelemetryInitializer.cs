using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Custom telemetry initializer to add custom properties to all telemetry.
    /// </summary>
    internal class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IConfiguration _configuration;

        public CustomTelemetryInitializer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Initialize(Microsoft.ApplicationInsights.Channel.ITelemetry telemetry)
        {
            // Set cloud role name
            var roleName = _configuration["ApplicationInsights:CloudRoleName"] ?? "TEDF.API";
            telemetry.Context.Cloud.RoleName = roleName;

            // Add custom properties
            if (!telemetry.Context.GlobalProperties.ContainsKey("Environment"))
            {
                var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
                telemetry.Context.GlobalProperties["Environment"] = environment;
            }

            // Add version
            if (!telemetry.Context.GlobalProperties.ContainsKey("Version"))
            {
                var version = typeof(CustomTelemetryInitializer).Assembly.GetName().Version?.ToString() ?? "1.0.0";
                telemetry.Context.GlobalProperties["Version"] = version;
            }
        }
    }
}
