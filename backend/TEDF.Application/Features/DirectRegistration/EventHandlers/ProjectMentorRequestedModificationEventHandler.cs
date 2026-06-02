using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Application.Features.DirectRegistration.EventHandlers;

public class ProjectMentorRequestedModificationEventHandler : INotificationHandler<ProjectMentorRequestedModificationEvent>
{
    private readonly ILogger<ProjectMentorRequestedModificationEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;

    public ProjectMentorRequestedModificationEventHandler(
        ILogger<ProjectMentorRequestedModificationEventHandler> logger,
        INotificationService notificationService,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
    }

    public async Task Handle(ProjectMentorRequestedModificationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Project {ProjectId} needs modification. Feedback: {Feedback}",
            notification.ProjectId, notification.Feedback);

        var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project?.GroupId is null) return;

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
