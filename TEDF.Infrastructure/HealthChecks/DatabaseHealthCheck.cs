using Microsoft.Extensions.Diagnostics.HealthChecks;
using TEDF.Persistence.SqlServer;

namespace TEDF.Infrastructure.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public DatabaseHealthCheck(AppDbContext context) => _context = context;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                await _context.Database.CanConnectAsync(ct);
                return HealthCheckResult.Healthy("SQL Server database is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SQL Server database is unhealthy.", ex);
            }
        }
    }
}
