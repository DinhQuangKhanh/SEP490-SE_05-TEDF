using Microsoft.Extensions.Logging;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class DataCleanupJob
    {
        private readonly ILogger<DataCleanupJob> _logger;

        public DataCleanupJob(ILogger<DataCleanupJob> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            _logger.LogInformation("Starting DataCleanupJob");
            // TODO: Implementation: Clean up old logs, temporary files, etc.
            _logger.LogInformation("DataCleanupJob completed");
            return Task.CompletedTask;
        }
    }
}
