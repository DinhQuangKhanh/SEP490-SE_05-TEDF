using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    /// <summary>
    /// Emails an evaluator when they are assigned to review a topic. One email per assignment,
    /// keyed on the assignment id, so re-running the assignment endpoint cannot send it twice.
    /// </summary>
    public class SendEvaluationAssignedMailHandler : INotificationHandler<EvaluatorAssignedToProjectEvent>
    {
        private readonly IProjectMailContextFactory _contextFactory;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendEvaluationAssignedMailHandler> _logger;

        public SendEvaluationAssignedMailHandler(
            IProjectMailContextFactory contextFactory,
            ISemesterRepository semesterRepository,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendEvaluationAssignedMailHandler> logger)
        {
            _contextFactory = contextFactory;
            _semesterRepository = semesterRepository;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(EvaluatorAssignedToProjectEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var context = await _contextFactory.CreateAsync(notification.ProjectId, cancellationToken);
                if (context is null) return;

                var evaluator = await _contextFactory.GetUserAsync(notification.EvaluatorId, cancellationToken);
                if (evaluator is null) return;

                var assignedBy = await _contextFactory.GetUserAsync(notification.AssignedBy, cancellationToken);

                var message = new TedfMailMessage
                {
                    To = evaluator.Email,
                    TemplateName = MailTemplateNames.EvaluationAssigned,
                    DedupeKey = $"evaluation-assigned:{notification.AssignmentId}",
                    Data = new Dictionary<string, string>
                    {
                        ["evaluatorName"] = evaluator.FullName,
                        ["topicName"] = context.ProjectName,
                        ["assignedBy"] = MailFormat.Text(assignedBy?.FullName),
                        ["deadline"] = await ResolveDeadlineAsync(context.SemesterId, notification.PhaseId, cancellationToken),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl($"/lecturer/moderate/{context.ProjectId}")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing evaluation-assigned email for Project {ProjectId}",
                    notification.ProjectId);
            }
        }

        /// <summary>
        /// An assignment has no deadline column of its own; the review must be finished by the end of
        /// the semester phase it was filed under.
        /// </summary>
        private async Task<string> ResolveDeadlineAsync(int semesterId, int phaseId, CancellationToken ct)
        {
            var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
            var phase = semester?.Phases.FirstOrDefault(p => p.Id == phaseId);
            return phase is null ? "Chưa ấn định" : MailFormat.Date(phase.EndDate);
        }
    }
}
