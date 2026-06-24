using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Infrastructure.Caching;
using TEDF.Infrastructure.RealTime.Services;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectRejectedEventHandler : INotificationHandler<ProjectRejectedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<ProjectRejectedEventHandler> _logger;

        public ProjectRejectedEventHandler(
            INotificationService notificationService,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            IProjectRepository projectRepository,
            IRealtimeNotificationService realtimeNotificationService,
            ICacheInvalidationService cacheInvalidation,
            ILogger<ProjectRejectedEventHandler> logger)
        {
            _notificationService = notificationService;
            _assignmentRepository = assignmentRepository;
            _projectRepository = projectRepository;
            _realtimeNotificationService = realtimeNotificationService;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        public async Task Handle(ProjectRejectedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate cache for all evaluators assigned to this project
                var assignments = await _assignmentRepository.GetActiveByProjectIdAsync(notification.ProjectId, cancellationToken);
                foreach (var assignment in assignments)
                {
                    await _cacheInvalidation.InvalidateEvaluatorCacheAsync(assignment.EvaluatorId, cancellationToken);
                }

                await ProjectStatusRealtimeNotifier.NotifyAsync(
                    _projectRepository, _realtimeNotificationService, notification.ProjectId, "PendingEvaluation", cancellationToken);

                _logger.LogInformation("Project rejected: {ProjectId}", notification.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(ProjectRejectedEvent), notification.ProjectId);
            }
        }
    }
}
