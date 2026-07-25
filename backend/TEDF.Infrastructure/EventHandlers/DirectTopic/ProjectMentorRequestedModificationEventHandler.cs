using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.EventHandlers.Project;
using TEDF.Infrastructure.RealTime.Services;

namespace TEDF.Infrastructure.EventHandlers.DirectTopic;

public class ProjectMentorRequestedModificationEventHandler : INotificationHandler<ProjectMentorRequestedModificationEvent>
{
    private readonly ILogger<ProjectMentorRequestedModificationEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IRealtimeNotificationService _realtimeNotificationService;

    public ProjectMentorRequestedModificationEventHandler(
        ILogger<ProjectMentorRequestedModificationEventHandler> logger,
        INotificationService notificationService,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        IRealtimeNotificationService realtimeNotificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _realtimeNotificationService = realtimeNotificationService;
    }

    public async Task Handle(ProjectMentorRequestedModificationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Project {ProjectId} needs modification. Feedback: {Feedback}",
            notification.ProjectId, notification.Feedback);

        var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project?.GroupId is null) return;

        // Push a real-time status update so the student's my-topic page refreshes live
        // (PendingMentorReview → NeedsModification).
        await ProjectStatusRealtimeNotifier.NotifyAsync(
            _projectRepository, _realtimeNotificationService, notification.ProjectId,
            "PendingMentorReview", cancellationToken);

        var group = await _groupRepository.GetWithMembersAsync(project.GroupId.Value, cancellationToken);
        if (group?.LeaderId is null) return;

        var feedbackText = string.IsNullOrWhiteSpace(notification.Feedback)
            ? ""
            : $" Góp ý: {notification.Feedback}";

        await _notificationService.SendAsync(
            group.LeaderId.Value,
            "Giảng viên yêu cầu chỉnh sửa đề tài",
            $"Đề tài \"{project.NameVi}\" cần được chỉnh sửa.{feedbackText}",
            NotificationType.Warning,
            NotificationCategory.Project,
            $"/student/my-topic",
            cancellationToken);
    }
}
