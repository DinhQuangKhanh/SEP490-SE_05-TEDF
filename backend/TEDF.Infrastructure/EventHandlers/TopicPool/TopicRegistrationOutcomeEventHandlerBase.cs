using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    /// <summary>
    /// Shared flow for the two "registration resolved" outcomes (confirmed / rejected): drop the
    /// item from the mentor tab (real-time) and notify the student group (bell + real-time refresh
    /// of their "Đề tài của tôi" page). Subclasses only supply the outcome-specific copy.
    /// </summary>
    public abstract class TopicRegistrationOutcomeEventHandlerBase
    {
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly IProjectRepository _projectRepository;
        private readonly IGroupRepository _groupRepository;

        protected TopicRegistrationOutcomeEventHandlerBase(
            ILogger logger,
            INotificationService notificationService,
            IRealtimeNotificationService realtime,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository)
        {
            _logger = logger;
            _notificationService = notificationService;
            _realtime = realtime;
            _projectRepository = projectRepository;
            _groupRepository = groupRepository;
        }

        /// <param name="messageFormat">Notification body; <c>{0}</c> is replaced with the topic name.</param>
        protected async Task NotifyOutcomeAsync(
            Guid registrationId,
            Guid projectId,
            Guid groupId,
            string outcomeLogLabel,
            string notificationTitle,
            string messageFormat,
            NotificationType notificationType,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration {Outcome}: {RegistrationId}, Project: {ProjectId}",
                outcomeLogLabel, registrationId, projectId);

            var payload = new { action = "removed", registrationId, projectId };

            var project = await _projectRepository.GetWithMentorsAsync(projectId, cancellationToken);
            var projectName = project is not null ? project.NameVi.ToString() : "đề tài";

            // Sync mentor tab(s).
            if (project is not null)
            {
                foreach (var mentorId in project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId))
                    await _realtime.SendToUserAsync(mentorId, "ReceiveRegistrationUpdate", payload, cancellationToken);
            }

            // Notify the student group + push real-time so their page refreshes.
            var group = await _groupRepository.GetWithMembersAsync(groupId, cancellationToken);
            if (group is null) return;

            var studentIds = group.Members.Where(m => m.IsActive).Select(m => m.StudentId).ToList();
            foreach (var studentId in studentIds)
                await _realtime.SendToUserAsync(studentId, "ReceiveRegistrationUpdate", payload, cancellationToken);

            if (studentIds.Count > 0)
            {
                await _notificationService.SendToMultipleAsync(
                    studentIds,
                    notificationTitle,
                    string.Format(messageFormat, projectName),
                    notificationType,
                    NotificationCategory.TopicPool,
                    "/student/my-topic",
                    cancellationToken);
            }
        }
    }
}
