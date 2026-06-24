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
    public class TopicRegistrationRejectedEventHandler
        : TopicRegistrationOutcomeEventHandlerBase, INotificationHandler<TopicRegistrationRejectedEvent>
    {
        public TopicRegistrationRejectedEventHandler(
            ILogger<TopicRegistrationRejectedEventHandler> logger,
            INotificationService notificationService,
            IRealtimeNotificationService realtime,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository)
            : base(logger, notificationService, realtime, projectRepository, groupRepository)
        {
        }

        public Task Handle(TopicRegistrationRejectedEvent notification, CancellationToken cancellationToken) =>
            NotifyOutcomeAsync(
                notification.RegistrationId,
                notification.ProjectId,
                notification.GroupId,
                "rejected",
                "Đăng ký đề tài bị từ chối",
                "Giảng viên đã từ chối đăng ký đề tài \"{0}\".",
                NotificationType.Warning,
                cancellationToken);
    }
}
