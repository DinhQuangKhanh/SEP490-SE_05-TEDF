using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Infrastructure.Caching;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class EvaluationCompletedEventHandler : INotificationHandler<EvaluationCompletedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IEvaluationLogRepository _evaluationLogRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<EvaluationCompletedEventHandler> _logger;

        public EvaluationCompletedEventHandler(
            INotificationService notificationService,
            IEvaluationLogRepository evaluationLogRepository,
            ICacheInvalidationService cacheInvalidation,
            ILogger<EvaluationCompletedEventHandler> logger)
        {
            _notificationService = notificationService;
            _evaluationLogRepository = evaluationLogRepository;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        public async Task Handle(EvaluationCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var evaluatorId = notification.EvaluatorId;

                await _evaluationLogRepository.AddAsync(new EvaluationLogDocument
                {
                    ProjectId = notification.ProjectId,
                    EvaluationSubmissionId = notification.SubmissionId,
                    Action = EvaluationAction.Completed,
                    Result = notification.Result,
                    PerformedBy = evaluatorId ?? Guid.Empty,
                    PerformedAt = DateTime.UtcNow
                }, cancellationToken);

                // Invalidate evaluator cache - dashboard, projects, history all change when evaluation completes
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
