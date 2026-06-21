using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    /// <summary>
    /// When a registration is rejected: drop it from the mentor tab (real-time), and notify the
    /// student group (bell + real-time refresh of their "Đề tài của tôi" page).
    /// </summary>
    public class TopicRegistrationRejectedEventHandler : INotificationHandler<TopicRegistrationRejectedEvent>
    {
        private readonly ILogger<TopicRegistrationRejectedEventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly IProjectRepository _projectRepository;
        private readonly IGroupRepository _groupRepository;

        public TopicRegistrationRejectedEventHandler(
            ILogger<TopicRegistrationRejectedEventHandler> logger,
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

        public async Task Handle(TopicRegistrationRejectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration rejected: {RegistrationId}, Project: {ProjectId}",
                notification.RegistrationId, notification.ProjectId);

            var payload = new { action = "removed", registrationId = notification.RegistrationId, projectId = notification.ProjectId };

            var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
            var projectName = project is not null ? project.NameVi.ToString() : "đề tài";

            // Sync mentor tab(s).
            if (project is not null)
            {
                foreach (var mentorId in project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId))
                    await _realtime.SendToUserAsync(mentorId, "ReceiveRegistrationUpdate", payload, cancellationToken);
            }

            // Notify the student group + push real-time so their page refreshes.
            var group = await _groupRepository.GetWithMembersAsync(notification.GroupId, cancellationToken);
            if (group is null) return;

            var studentIds = group.Members.Where(m => m.IsActive).Select(m => m.StudentId).ToList();
            foreach (var studentId in studentIds)
                await _realtime.SendToUserAsync(studentId, "ReceiveRegistrationUpdate", payload, cancellationToken);

            if (studentIds.Count > 0)
            {
                await _notificationService.SendToMultipleAsync(
                    studentIds,
                    "Đăng ký đề tài bị từ chối",
                    $"Giảng viên đã từ chối đăng ký đề tài \"{projectName}\".",
                    NotificationType.Warning,
                    NotificationCategory.TopicPool,
                    "/student/my-topic",
                    cancellationToken);
            }
        }
    }
}
