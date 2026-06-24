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
    /// When a registration is confirmed: drop it from the mentor tab (real-time), and notify the
    /// student group (bell + real-time refresh of their "Đề tài của tôi" page).
    /// </summary>
    public class TopicRegistrationConfirmedEventHandler
        : TopicRegistrationOutcomeEventHandlerBase, INotificationHandler<TopicRegistrationConfirmedEvent>
    {
        public TopicRegistrationConfirmedEventHandler(
            ILogger<TopicRegistrationConfirmedEventHandler> logger,
            INotificationService notificationService,
            IRealtimeNotificationService realtime,
            IProjectRepository projectRepository,
            IGroupRepository groupRepository)
            : base(logger, notificationService, realtime, projectRepository, groupRepository)
        {
        }

        public Task Handle(TopicRegistrationConfirmedEvent notification, CancellationToken cancellationToken) =>
            NotifyOutcomeAsync(
                notification.RegistrationId,
                notification.ProjectId,
                notification.GroupId,
                "confirmed",
                "Đăng ký đề tài được chấp nhận",
                "Giảng viên đã chấp nhận đăng ký đề tài \"{0}\".",
                NotificationType.Success,
                cancellationToken);
    }
}
