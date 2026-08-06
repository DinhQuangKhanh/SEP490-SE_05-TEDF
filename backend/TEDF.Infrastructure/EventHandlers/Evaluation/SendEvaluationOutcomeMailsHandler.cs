using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    /// <summary>
    /// Emails triggered by an evaluator submitting their result:
    /// <list type="bullet">
    /// <item>always — <c>evaluation-completed</c> to the proposing lecturer and the department head;</item>
    /// <item>once every required evaluator has submitted and they agree —
    /// <c>evaluation-consensus-approved</c> / <c>evaluation-consensus-rejected</c> to the lecturer and
    /// the registered group's students. Conflicting conclusions send nothing: the topic waits for the
    /// department head's final decision.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// The event fires only from <c>SubmitEvaluationAsync</c>, and
    /// <c>ProjectEvaluatorAssignment.SubmitEvaluation</c> throws once a result exists — so this runs
    /// exactly on the not-completed → completed transition, never on a feedback edit, a re-read or a
    /// repeated call. Dedupe keys additionally carry the evaluation round so a resubmitted topic can
    /// legitimately produce a new round of emails.
    /// </remarks>
    public class SendEvaluationOutcomeMailsHandler : INotificationHandler<EvaluatorSubmittedResultEvent>
    {
        private readonly IProjectMailContextFactory _contextFactory;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly ISystemSettingsService _settings;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendEvaluationOutcomeMailsHandler> _logger;

        public SendEvaluationOutcomeMailsHandler(
            IProjectMailContextFactory contextFactory,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            ISystemSettingsService settings,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendEvaluationOutcomeMailsHandler> logger)
        {
            _contextFactory = contextFactory;
            _assignmentRepository = assignmentRepository;
            _settings = settings;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(EvaluatorSubmittedResultEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var context = await _contextFactory.CreateAsync(notification.ProjectId, cancellationToken);
                if (context is null) return;

                var evaluator = await _contextFactory.GetUserAsync(notification.EvaluatorId, cancellationToken);

                var messages = BuildCompletedMessages(notification, context, evaluator);
                messages.AddRange(await BuildConsensusMessagesAsync(notification, context, cancellationToken));

                if (messages.Count == 0) return;
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing evaluation outcome emails for Project {ProjectId}",
                    notification.ProjectId);
            }
        }

        /// <summary>One "an evaluator has finished" email each to the lecturer and the department head.</summary>
        private List<TedfMailMessage> BuildCompletedMessages(
            EvaluatorSubmittedResultEvent notification, ProjectMailContext context, MailRecipient? evaluator)
        {
            var detailUrl = _mailQueue.BuildDetailUrl($"/lecturer/moderate/{context.ProjectId}");
            var completedAt = MailFormat.DateTimeText(DateTime.UtcNow);
            var evaluatorName = MailFormat.Text(evaluator?.FullName);
            var conclusion = MailFormat.Result(notification.Result);

            return Recipients(context.Mentor, context.DepartmentHead)
                .Select(recipient => new TedfMailMessage
                {
                    To = recipient.Email,
                    TemplateName = MailTemplateNames.EvaluationCompleted,
                    DedupeKey = $"evaluation-completed:{notification.AssignmentId}:{context.Round}:{recipient.UserId}",
                    Data = new Dictionary<string, string>
                    {
                        ["recipientName"] = recipient.FullName,
                        ["evaluatorName"] = evaluatorName,
                        ["topicName"] = context.ProjectName,
                        ["completedAt"] = completedAt,
                        ["evaluationConclusion"] = conclusion,
                        ["detailUrl"] = detailUrl
                    }
                })
                .ToList();
        }

        /// <summary>
        /// The "everyone agreed" email, sent only after every required evaluator has submitted.
        /// Returns nothing while a submission is still outstanding or the conclusions differ.
        /// </summary>
        private async Task<List<TedfMailMessage>> BuildConsensusMessagesAsync(
            EvaluatorSubmittedResultEvent notification, ProjectMailContext context, CancellationToken ct)
        {
            var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(notification.ProjectId, ct)).ToList();
            var submitted = assignments.Where(a => a.HasSubmittedEvaluation).ToList();

            // Mirrors the auto-resolve rule in EvaluationsDomainService.SubmitEvaluationAsync.
            if (submitted.Count < 2 || submitted.Count < assignments.Count) return [];

            var results = submitted.Select(a => a.IndividualResult!.Value).Distinct().ToList();
            if (results.Count != 1) return [];

            // Same admin switch that gates the in-app result notification (Settings → Notifications).
            var notifyOnResult = await _settings.GetBoolAsync(SettingKeys.EmailOnEvaluationResult, true, ct);
            if (!notifyOnResult)
            {
                _logger.LogInformation(
                    "Consensus email for Project {ProjectId} skipped: {Setting} is off.",
                    context.ProjectId, SettingKeys.EmailOnEvaluationResult);
                return [];
            }

            var agreedResult = results[0];
            // Only "approved" and "rejected" templates exist. Anything that is not an approval is a
            // non-approval; the exact wording ("Yêu cầu chỉnh sửa" vs "Không duyệt đề tài") is carried
            // by the conclusion placeholder.
            var templateName = agreedResult == EvaluationResult.Approved
                ? MailTemplateNames.EvaluationConsensusApproved
                : MailTemplateNames.EvaluationConsensusRejected;

            var conclusion = MailFormat.Result(agreedResult);
            var lecturerUrl = _mailQueue.BuildDetailUrl($"/lecturer/moderate/{context.ProjectId}");
            var studentUrl = _mailQueue.BuildDetailUrl("/student/my-topic");

            var messages = Recipients(context.Mentor)
                .Select(m => BuildConsensusMessage(context, m, templateName, conclusion, lecturerUrl))
                .ToList();

            // Students are included only when a group has actually registered the topic and the
            // members carry an address — an unclaimed pool topic simply has nobody to tell.
            messages.AddRange(context.Students
                .Select(s => BuildConsensusMessage(context, s, templateName, conclusion, studentUrl)));

            return messages;
        }

        private static TedfMailMessage BuildConsensusMessage(
            ProjectMailContext context, MailRecipient recipient, string templateName, string conclusion, string detailUrl) =>
            new()
            {
                To = recipient.Email,
                TemplateName = templateName,
                DedupeKey = $"evaluation-consensus:{context.ProjectId}:{context.Round}:{recipient.UserId}",
                Data = new Dictionary<string, string>
                {
                    ["recipientName"] = recipient.FullName,
                    ["topicName"] = context.ProjectName,
                    ["conclusion"] = conclusion,
                    ["detailUrl"] = detailUrl
                }
            };

        /// <summary>Drops the people who could not be resolved and collapses anyone holding two roles.</summary>
        private static IEnumerable<MailRecipient> Recipients(params MailRecipient?[] candidates) =>
            candidates.OfType<MailRecipient>().DistinctBy(r => r.UserId);
    }
}
