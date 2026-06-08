using Microsoft.Extensions.Logging;
using TEDF.Domain.Services;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class EvaluationReminderJob
    {
        private readonly IEvaluationsDomainService _evaluationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EvaluationReminderJob> _logger;

        public EvaluationReminderJob(
            IEvaluationsDomainService evaluationService,
            INotificationService notificationService,
            ILogger<EvaluationReminderJob> logger)
        {
            _evaluationService = evaluationService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public Task ExecuteAsync()
        {
            _logger.LogInformation("Starting EvaluationReminderJob");
            // TODO: Implementation: Send reminders for pending evaluations
            _logger.LogInformation("EvaluationReminderJob completed");
            return Task.CompletedTask;
        }
    }
}
