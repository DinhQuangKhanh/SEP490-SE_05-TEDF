using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    /// <summary>
    /// Emails the proposing lecturer once the department head has broken a tie between the
    /// evaluators. Keyed on project + evaluation round, so calling the decision endpoint again for
    /// the same round cannot produce a second email.
    /// </summary>
    public class SendFinalDecisionMailHandler : INotificationHandler<DepartmentHeadFinalDecisionEvent>
    {
        private readonly IProjectMailContextFactory _contextFactory;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendFinalDecisionMailHandler> _logger;

        public SendFinalDecisionMailHandler(
            IProjectMailContextFactory contextFactory,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendFinalDecisionMailHandler> logger)
        {
            _contextFactory = contextFactory;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(DepartmentHeadFinalDecisionEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var context = await _contextFactory.CreateAsync(notification.ProjectId, cancellationToken);
                if (context?.Mentor is null) return;

                var decidedBy = await _contextFactory.GetUserAsync(notification.DecidedBy, cancellationToken);

                var message = new TedfMailMessage
                {
                    To = context.Mentor.Email,
                    TemplateName = MailTemplateNames.TopicFinalDecision,
                    DedupeKey = $"topic-final-decision:{context.ProjectId}:{context.Round}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = context.Mentor.FullName,
                        ["topicName"] = context.ProjectName,
                        ["finalDecision"] = MailFormat.Result(notification.FinalResult),
                        ["decisionReason"] = MailFormat.Text(notification.Notes, "Không có ghi chú"),
                        ["decidedBy"] = MailFormat.Text(decidedBy?.FullName),
                        ["decidedAt"] = MailFormat.DateTimeText(DateTime.UtcNow),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl($"/lecturer/moderate/{context.ProjectId}")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing topic-final-decision email for Project {ProjectId}",
                    notification.ProjectId);
            }
        }
    }
}
