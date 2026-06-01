using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using TEDF.Persistence.MongoDB;

namespace TEDF.Infrastructure.HealthChecks
{
    public class MongoDbHealthCheck : IHealthCheck
    {
        private readonly MongoDbContext _context;

        public MongoDbHealthCheck(MongoDbContext context) => _context = context;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                var collection = _context.GetCollection<MongoDB.Bson.BsonDocument>("health_check");
                await collection.Find(_ => true).Limit(1).FirstOrDefaultAsync(ct);
                return HealthCheckResult.Healthy("MongoDB is healthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB is unhealthy.", ex);
            }
        }
    }
}
