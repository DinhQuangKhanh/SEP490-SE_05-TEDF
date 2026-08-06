using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Infrastructure.BackgroundJobs.Jobs;

namespace TEDF.Infrastructure.EventHandlers.Semesters
{
    /// <summary>
    /// On roster publish: enqueues the job that emails every eligible student and every assigned
    /// lecturer, so the mail work runs off the request thread.
    /// </summary>
    public class EnqueueRosterMailsOnRosterPublishedHandler : INotificationHandler<SemesterRosterPublishedEvent>
    {
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<EnqueueRosterMailsOnRosterPublishedHandler> _logger;

        public EnqueueRosterMailsOnRosterPublishedHandler(
            IBackgroundJobService backgroundJobs,
            ILogger<EnqueueRosterMailsOnRosterPublishedHandler> logger)
        {
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public Task Handle(SemesterRosterPublishedEvent notification, CancellationToken cancellationToken)
        {
            var semesterId = notification.SemesterId;
            try
            {
                _backgroundJobs.Enqueue<SendRosterPublishedMailJob>(job => job.ExecuteAsync(semesterId));
                _logger.LogInformation("Enqueued roster emails for Semester {SemesterId}.", semesterId);
            }
            catch (Exception ex)
            {
                // The roster is already published and committed; failing to schedule the mail job
                // must not turn a successful publish into an error response.
                _logger.LogError(ex, "Error enqueueing roster emails for Semester {SemesterId}.", semesterId);
            }

            return Task.CompletedTask;
        }
    }
}
