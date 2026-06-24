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
    /// When a group requests to register for a pool topic, notify the topic's mentor(s) (bell)
    /// and push a real-time "added" update so an open registration tab updates live.
    /// </summary>
    public class TopicRegistrationRequestedEventHandler : INotificationHandler<TopicRegistrationRequestedEvent>
    {
        private readonly ILogger<TopicRegistrationRequestedEventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly IProjectRepository _projectRepository;

        public TopicRegistrationRequestedEventHandler(
            ILogger<TopicRegistrationRequestedEventHandler> logger,
            INotificationService notificationService,
            IRealtimeNotificationService realtime,
            IProjectRepository projectRepository)
        {
            _logger = logger;
            _notificationService = notificationService;
            _realtime = realtime;
            _projectRepository = projectRepository;
        }

        public async Task Handle(TopicRegistrationRequestedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration requested: {RegistrationId}, Project: {ProjectId}",
                notification.RegistrationId, notification.ProjectId);

            var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
            if (project is null) return;

            var mentorIds = project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId).ToList();
            if (mentorIds.Count == 0) return;

            await _notificationService.SendToMultipleAsync(
                mentorIds,
                "Yêu cầu đăng ký đề tài mới",
                $"Một nhóm sinh viên đã đăng ký đề tài \"{project.NameVi}\". Vui lòng xem xét và xác nhận.",
                NotificationType.Info,
                NotificationCategory.TopicPool,
                "/lecturer/registrations",
                cancellationToken);

            foreach (var mentorId in mentorIds)
            {
                await _realtime.SendToUserAsync(
                    mentorId,
                    "ReceiveRegistrationUpdate",
                    new { action = "added", registrationId = notification.RegistrationId, projectId = notification.ProjectId },
                    cancellationToken);
            }
        }
    }
}
