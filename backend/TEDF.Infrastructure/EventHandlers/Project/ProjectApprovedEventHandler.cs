using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Infrastructure.Caching;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectApprovedEventHandler : INotificationHandler<ProjectApprovedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<ProjectApprovedEventHandler> _logger;

        public ProjectApprovedEventHandler(
            INotificationService notificationService,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            ICacheInvalidationService cacheInvalidation,
            ILogger<ProjectApprovedEventHandler> logger)
        {
            _notificationService = notificationService;
            _assignmentRepository = assignmentRepository;
            _cacheInvalidation = cacheInvalidation;
            _logger = logger;
        }

        public async Task Handle(ProjectApprovedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Invalidate cache for all evaluators assigned to this project
                var assignments = await _assignmentRepository.GetActiveByProjectIdAsync(notification.ProjectId, cancellationToken);
                foreach (var assignment in assignments)
                {
                    await _cacheInvalidation.InvalidateEvaluatorCacheAsync(assignment.EvaluatorId, cancellationToken);
                }

                _logger.LogInformation("Project approved: {ProjectId}", notification.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(ProjectApprovedEvent), notification.ProjectId);
            }
        }
    }
}
