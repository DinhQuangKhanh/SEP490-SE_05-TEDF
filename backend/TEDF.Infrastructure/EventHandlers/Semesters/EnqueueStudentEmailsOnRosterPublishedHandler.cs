using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Infrastructure.BackgroundJobs.Jobs;

namespace TEDF.Infrastructure.EventHandlers.Semesters
{
    /// <summary>
    /// On roster publish: enqueues the bulk eligible-student email job so SMTP work runs off the request thread.
    /// </summary>
    public class EnqueueStudentEmailsOnRosterPublishedHandler : INotificationHandler<SemesterRosterPublishedEvent>
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<EnqueueStudentEmailsOnRosterPublishedHandler> _logger;

        public EnqueueStudentEmailsOnRosterPublishedHandler(
            IBackgroundJobClient backgroundJobClient,
            ILogger<EnqueueStudentEmailsOnRosterPublishedHandler> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public Task Handle(SemesterRosterPublishedEvent notification, CancellationToken cancellationToken)
        {
            _backgroundJobClient.Enqueue<SendEligibleStudentEmailsJob>(job => job.ExecuteAsync(notification.SemesterId));
            _logger.LogInformation("Enqueued eligible-student emails for Semester {SemesterId}.", notification.SemesterId);
            return Task.CompletedTask;
        }
    }
}
