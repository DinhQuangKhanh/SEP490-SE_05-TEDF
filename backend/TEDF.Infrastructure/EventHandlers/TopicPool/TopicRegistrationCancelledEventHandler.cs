using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    /// <summary>
    /// When a group cancels its registration, push a real-time "removed" update so the mentor's
    /// open registration tab drops the item live, and send a bell notification.
    /// </summary>
    public class TopicRegistrationCancelledEventHandler : INotificationHandler<TopicRegistrationCancelledEvent>
    {
        private readonly ILogger<TopicRegistrationCancelledEventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly IProjectRepository _projectRepository;

        public TopicRegistrationCancelledEventHandler(
            ILogger<TopicRegistrationCancelledEventHandler> logger,
            INotificationService notificationService,
            IRealtimeNotificationService realtime,
            IProjectRepository projectRepository)
        {
            _logger = logger;
            _notificationService = notificationService;
            _realtime = realtime;
            _projectRepository = projectRepository;
        }

        public async Task Handle(TopicRegistrationCancelledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration cancelled: {RegistrationId}, Project: {ProjectId}",
                notification.RegistrationId, notification.ProjectId);

            var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
            if (project is null) return;

            var mentorIds = project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId).ToList();
            if (mentorIds.Count == 0) return;

            foreach (var mentorId in mentorIds)
            {
                await _realtime.SendToUserAsync(
                    mentorId,
                    "ReceiveRegistrationUpdate",
                    new { action = "removed", registrationId = notification.RegistrationId, projectId = notification.ProjectId },
                    cancellationToken);
            }

            await _notificationService.SendToMultipleAsync(
                mentorIds,
                "Sinh viên đã huỷ đăng ký",
                $"Một nhóm sinh viên đã huỷ yêu cầu đăng ký đề tài \"{project.NameVi}\".",
                NotificationType.Info,
                NotificationCategory.TopicPool,
                "/lecturer/registrations",
                cancellationToken);
        }
    }
}
