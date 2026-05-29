using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TEDF.Infrastructure.HealthChecks
{
    public class ExternalServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly string _serviceName;

        public ExternalServiceHealthCheck(HttpClient httpClient, string serviceUrl, string serviceName)
        {
            _httpClient = httpClient;
            _serviceUrl = serviceUrl;
            _serviceName = serviceName;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(_serviceUrl, ct);
                if (response.IsSuccessStatusCode)
                    return HealthCheckResult.Healthy($"{_serviceName} is healthy.");
                return HealthCheckResult.Degraded($"{_serviceName} returned {response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"{_serviceName} is unhealthy.", ex);
            }
        }
    }
}
