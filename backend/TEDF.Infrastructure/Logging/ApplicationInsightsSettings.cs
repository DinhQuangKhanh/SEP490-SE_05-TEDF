namespace TEDF.Infrastructure.Logging
{
    /// <summary>
    /// Application Insights configuration settings.
    /// </summary>
    public class ApplicationInsightsSettings
    {
        public bool Enabled { get; set; } = false;
        public string? ConnectionString { get; set; }
        public bool EnableAdaptiveSampling { get; set; } = true;
        public bool EnableLiveMetrics { get; set; } = true;
        public bool EnableDependencyTracking { get; set; } = true;
        public bool EnableRequestTracking { get; set; } = true;
        public bool EnableSqlCommandTextInstrumentation { get; set; } = false;
        public bool FilterHealthCheckRequests { get; set; } = true;
        public string CloudRoleName { get; set; } = "TEDF.API";
    }
}
