using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Infrastructure.Caching;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectSubmittedEventHandler : INotificationHandler<ProjectSubmittedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IEvaluationLogRepository _evaluationLogRepository;
        private readonly IUserActivityLogRepository _activityLogRepository;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<ProjectSubmittedEventHandler> _logger;

        public ProjectSubmittedEventHandler(
            INotificationService notificationService,
            IEvaluationLogRepository evaluationLogRepository,
            IUserActivityLogRepository activityLogRepository,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            ICacheInvalidationService cacheInvalidation,
            ILogger<ProjectSubmittedEventHandler> logger)
        {
            _notificationService = notificationService;
            _evaluationLogRepository = evaluationLogRepository;
            _activityLogRepository = activityLogRepository;
            _assignmentRepository = assignmentRepository;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        public async Task Handle(ProjectSubmittedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _evaluationLogRepository.AddAsync(new EvaluationLogDocument
                {
                    ProjectId = notification.ProjectId,
                    Action = EvaluationAction.Submitted,
                    PerformedBy = notification.SubmittedBy,
                    PerformedAt = DateTime.UtcNow,
                }, cancellationToken);

                await _activityLogRepository.AddAsync(new UserActivityLogDocument
                {
                    UserId   = notification.SubmittedBy,
                    ActiveRole = "student",
                    Action   = "Nộp đồ án",
                    ActionCode = "ProjectSubmitted",
                    Category = "Project",
                    EntityType = "Project",
                    EntityId = notification.ProjectId,
                    Severity = "info",
                    Timestamp = DateTime.UtcNow,
                }, cancellationToken);

                // Invalidate cache for all evaluators assigned to this project
                var assignments = await _assignmentRepository.GetActiveByProjectIdAsync(notification.ProjectId, cancellationToken);
                foreach (var assignment in assignments)
                {
                    await _cacheInvalidation.InvalidateEvaluatorCacheAsync(assignment.EvaluatorId, cancellationToken);
                }

                _logger.LogInformation("Project submitted for evaluation: {ProjectId}", notification.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(ProjectSubmittedEvent), notification.ProjectId);
            }
        }
    }
}
