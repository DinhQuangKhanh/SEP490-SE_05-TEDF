using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate.Events;
using TEDF.Infrastructure.RealTime;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.EvaluationChecklist;

/// <summary>
/// Pushes a lightweight real-time signal to the project group when an evaluator saves their checklist, so
/// any open review screen for that project reloads the official data from the API. Reuses the existing
/// NotificationHub project group + <see cref="IRealtimeNotificationService"/> — no new hub/connection. Only
/// runs after the unit of work commits (domain events are dispatched post-save), and never carries the
/// scores/comments themselves (the client refetches).
/// </summary>
public sealed class ProjectChecklistSavedRealtimeHandler : INotificationHandler<ProjectChecklistSavedEvent>
{
    private readonly IRealtimeNotificationService _realtime;
    private readonly ILogger<ProjectChecklistSavedRealtimeHandler> _logger;

    public ProjectChecklistSavedRealtimeHandler(
        IRealtimeNotificationService realtime,
        ILogger<ProjectChecklistSavedRealtimeHandler> logger)
    {
        _realtime = realtime;
        _logger = logger;
    }

    public async Task Handle(ProjectChecklistSavedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _realtime.SendToProjectGroupAsync(
                notification.ProjectId,
                RealtimeEvents.ChecklistUpdated,
                new { projectId = notification.ProjectId },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Real-time delivery is best-effort; a failure must not break the save transaction.
            _logger.LogWarning(ex, "Failed to broadcast ChecklistUpdated for project {ProjectId}", notification.ProjectId);
        }
    }
}
