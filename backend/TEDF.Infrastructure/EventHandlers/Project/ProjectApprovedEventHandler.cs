using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Infrastructure.Caching;
using TEDF.Infrastructure.RealTime.Models;
using TEDF.Infrastructure.RealTime.Services;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectApprovedEventHandler : INotificationHandler<ProjectApprovedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly ILogger<ProjectApprovedEventHandler> _logger;

        public ProjectApprovedEventHandler(
            INotificationService notificationService,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            IProjectRepository projectRepository,
            IRealtimeNotificationService realtimeNotificationService,
            ICacheInvalidationService cacheInvalidation,
            ILogger<ProjectApprovedEventHandler> logger)
        {
            _notificationService = notificationService;
            _assignmentRepository = assignmentRepository;
            _projectRepository = projectRepository;
            _realtimeNotificationService = realtimeNotificationService;
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

                var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
                if (project is not null)
                {
                    await _realtimeNotificationService.SendProjectStatusUpdateAsync(
                        notification.ProjectId,
                        new ProjectStatusUpdate(
                            notification.ProjectId,
                            project.NameVi.Value,
                            "PendingEvaluation",
                            project.Status.ToString(),
                            DateTime.UtcNow),
                        cancellationToken);
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
