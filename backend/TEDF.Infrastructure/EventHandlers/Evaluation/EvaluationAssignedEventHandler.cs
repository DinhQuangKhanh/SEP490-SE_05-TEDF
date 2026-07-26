using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class EvaluationAssignedEventHandler : INotificationHandler<EvaluatorAssignedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<EvaluationAssignedEventHandler> _logger;

        public EvaluationAssignedEventHandler(
            INotificationService notificationService,
            ILogger<EvaluationAssignedEventHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public Task Handle(EvaluatorAssignedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Evaluator assigned: {EvaluatorId} to project {ProjectId}", notification.EvaluatorId, notification.ProjectId);
            return Task.CompletedTask;
        }
    }
}
