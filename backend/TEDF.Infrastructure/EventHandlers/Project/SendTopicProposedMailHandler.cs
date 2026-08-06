using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Enums.Project;
using TEDF.Infrastructure.BackgroundJobs.Jobs;
using TEDF.Infrastructure.Services.Email.Firestore;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    /// <summary>
    /// Emails the head of the owning department when a lecturer proposes a new topic.
    /// Runs alongside <see cref="ProjectCreatedEventHandler"/>, which raises the in-app notification.
    /// </summary>
    public class SendTopicProposedMailHandler : INotificationHandler<ProjectCreatedEvent>
    {
        private readonly IProjectMailContextFactory _contextFactory;
        private readonly IFirestoreMailQueue _mailQueue;
        private readonly IBackgroundJobService _backgroundJobs;
        private readonly ILogger<SendTopicProposedMailHandler> _logger;

        public SendTopicProposedMailHandler(
            IProjectMailContextFactory contextFactory,
            IFirestoreMailQueue mailQueue,
            IBackgroundJobService backgroundJobs,
            ILogger<SendTopicProposedMailHandler> logger)
        {
            _contextFactory = contextFactory;
            _mailQueue = mailQueue;
            _backgroundJobs = backgroundJobs;
            _logger = logger;
        }

        public async Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
        {
            // Only a mentor-proposed pool topic goes to the department head; a student-created topic
            // travels to its mentor first and is not a proposal to the department.
            if (notification.SourceType != ProjectSourceType.FromPool) return;

            try
            {
                var context = await _contextFactory.CreateAsync(notification.ProjectId, cancellationToken);
                if (context?.DepartmentHead is null) return;

                var message = new TedfMailMessage
                {
                    To = context.DepartmentHead.Email,
                    TemplateName = MailTemplateNames.TopicProposed,
                    DedupeKey = $"topic-proposed:{context.ProjectId}",
                    Data = new Dictionary<string, string>
                    {
                        ["departmentHeadName"] = context.DepartmentHead.FullName,
                        ["lecturerName"] = MailFormat.Text(context.Mentor?.FullName),
                        ["topicName"] = context.ProjectName,
                        ["departmentName"] = MailFormat.Text(context.DepartmentName, "Bộ môn"),
                        ["proposedAt"] = MailFormat.DateTimeText(context.CreatedAtUtc),
                        ["detailUrl"] = _mailQueue.BuildDetailUrl("/lecturer/assign")
                    }
                };

                var messages = new List<TedfMailMessage> { message };
                _backgroundJobs.Enqueue<MailDispatchJob>(job => job.ExecuteAsync(messages));
            }
            catch (Exception ex)
            {
                // The topic is already committed; a mail problem must not surface as a failed request.
                _logger.LogError(ex, "Error queueing topic-proposed email for Project {ProjectId}",
                    notification.ProjectId);
            }
        }
    }
}
