using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Infrastructure.Caching;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class EvaluationCompletedEventHandler : INotificationHandler<EvaluationCompletedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<EvaluationCompletedEventHandler> _logger;

        public EvaluationCompletedEventHandler(
            INotificationService notificationService,
            ICacheInvalidationService cacheInvalidation,
            ILogger<EvaluationCompletedEventHandler> logger)
        {
            _notificationService = notificationService;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        public async Task Handle(EvaluationCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var evaluatorId = notification.EvaluatorId;

                if (evaluatorId.HasValue)
                {
                    await _cacheInvalidation.InvalidateEvaluatorCacheAsync(evaluatorId.Value, cancellationToken);
                }

                _logger.LogInformation("Evaluation completed: {ProjectId}, Result: {Result}", notification.ProjectId, notification.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(EvaluationCompletedEvent), notification.ProjectId);
            }
        }
    }
}
