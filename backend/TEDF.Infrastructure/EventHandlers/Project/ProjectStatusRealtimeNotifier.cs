using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Infrastructure.RealTime.Models;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    /// <summary>
    /// Shared helper for project status-changed event handlers: re-fetches the project
    /// and pushes a <see cref="ProjectStatusUpdate"/> over SignalR to the project's group.
    /// </summary>
    internal static class ProjectStatusRealtimeNotifier
    {
        public static async Task NotifyAsync(
            IProjectRepository projectRepository,
            IRealtimeNotificationService realtimeNotificationService,
            Guid projectId,
            string oldStatus,
            CancellationToken cancellationToken)
        {
            var project = await projectRepository.GetByIdAsync(projectId, cancellationToken);
            if (project is null)
            {
                return;
            }

            await realtimeNotificationService.SendProjectStatusUpdateAsync(
                projectId,
                new ProjectStatusUpdate(
                    projectId,
                    project.NameVi.Value,
                    oldStatus,
                    project.Status.ToString(),
                    DateTime.UtcNow),
                cancellationToken);
        }
    }
}
