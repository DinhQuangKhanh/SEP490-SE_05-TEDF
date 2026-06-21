using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Infrastructure.RealTime.Models;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectModificationRequestedEventHandler : INotificationHandler<ProjectModificationRequestedEvent>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly ILogger<ProjectModificationRequestedEventHandler> _logger;

        public ProjectModificationRequestedEventHandler(
            IProjectRepository projectRepository,
            IRealtimeNotificationService realtimeNotificationService,
            ILogger<ProjectModificationRequestedEventHandler> logger)
        {
            _projectRepository = projectRepository;
            _realtimeNotificationService = realtimeNotificationService;
            _logger = logger;
        }

        public async Task Handle(ProjectModificationRequestedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
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

                _logger.LogInformation("Project modification requested: {ProjectId}", notification.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(ProjectModificationRequestedEvent), notification.ProjectId);
            }
        }
    }
}
