using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class EvaluatorAssignedToProjectEventHandler : INotificationHandler<EvaluatorAssignedToProjectEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectRepository _projectRepository;
        private readonly ILogger<EvaluatorAssignedToProjectEventHandler> _logger;

        public EvaluatorAssignedToProjectEventHandler(
            INotificationService notificationService,
            IProjectRepository projectRepository,
            ILogger<EvaluatorAssignedToProjectEventHandler> logger)
        {
            _notificationService = notificationService;
            _projectRepository = projectRepository;
            _logger = logger;
        }

        public async Task Handle(EvaluatorAssignedToProjectEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
                var projectName = project?.NameVi.Value ?? "Không xác định";

                await _notificationService.SendAsync(
                    notification.EvaluatorId,
                    "Phân công thẩm định",
                    $"Bạn được phân công thẩm định đề tài '{projectName}'.",
                    NotificationType.Info,
                    NotificationCategory.Evaluation,
                    $"/lecturer/moderate/{notification.ProjectId}",
                    cancellationToken);

                _logger.LogInformation(
                    "Evaluator {EvaluatorId} assigned to project {ProjectId} (order {Order})",
                    notification.EvaluatorId, notification.ProjectId, notification.EvaluatorOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(EvaluatorAssignedToProjectEvent), notification.ProjectId);
            }
        }
    }
}
