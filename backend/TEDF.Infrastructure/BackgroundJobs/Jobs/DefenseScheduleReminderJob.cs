using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class DefenseScheduleReminderJob
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<DefenseScheduleReminderJob> _logger;

        public DefenseScheduleReminderJob(INotificationService notificationService, ILogger<DefenseScheduleReminderJob> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            _logger.LogInformation("Starting DefenseScheduleReminderJob");
            // TODO: Implementation: Send reminders for upcoming defense schedules
            _logger.LogInformation("DefenseScheduleReminderJob completed");
            return Task.CompletedTask;
        }
    }
}
