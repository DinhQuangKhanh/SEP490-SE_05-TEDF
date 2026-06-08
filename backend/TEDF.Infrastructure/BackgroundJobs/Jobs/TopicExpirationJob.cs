using Microsoft.Extensions.Logging;
using TEDF.Domain.Services;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class TopicExpirationJob
    {
        private readonly ITopicPoolsDomainService _topicPoolService;
        private readonly ISemestersDomainService _semesterService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TopicExpirationJob> _logger;

        public TopicExpirationJob(
            ITopicPoolsDomainService topicPoolService,
            ISemestersDomainService semesterService,
            INotificationService notificationService,
            ILogger<TopicExpirationJob> logger)
        {
            _topicPoolService = topicPoolService;
            _semesterService = semesterService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting TopicExpirationJob");

            var resolvedCount = await _topicPoolService.ResolveMissingExpirationSemestersAsync();
            _logger.LogInformation("Resolved expiration semester for {Count} topics before expiration check", resolvedCount);

            var currentSemesterId = await _semesterService.GetActiveSemesterIdAsync();
            if (currentSemesterId is null)
            {
                _logger.LogWarning("No active semester found");
                return;
            }

            var expiredCount = await _topicPoolService.ExpireOldTopicsAsync(currentSemesterId.Value);
            _logger.LogInformation("Expired {Count} topics", expiredCount);

            // Notify about expiring topics
            var expiringTopics = await _topicPoolService.GetExpiringTopicsAsync(currentSemesterId.Value);
            foreach (var topic in expiringTopics)
            {
                await _notificationService.SendTopicExpirationWarningAsync(topic);
            }

            _logger.LogInformation("TopicExpirationJob completed");
        }
    }
}
