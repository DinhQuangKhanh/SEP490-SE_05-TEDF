using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.DirectTopic;

public class ProjectSubmittedToMentorEventHandler : INotificationHandler<ProjectSubmittedToMentorEvent>
{
    private readonly ILogger<ProjectSubmittedToMentorEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IProjectRepository _projectRepository;

    public ProjectSubmittedToMentorEventHandler(
        ILogger<ProjectSubmittedToMentorEventHandler> logger,
        INotificationService notificationService,
        IProjectRepository projectRepository)
    {
        _logger = logger;
        _notificationService = notificationService;
        _projectRepository = projectRepository;
    }

    public async Task Handle(ProjectSubmittedToMentorEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Project {ProjectId} submitted to mentor by {SubmittedBy}",
            notification.ProjectId, notification.SubmittedBy);

        var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
        if (project is null) return;

        var mentorIds = project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId).ToList();
        if (mentorIds.Count == 0) return;

        await _notificationService.SendToMultipleAsync(
            mentorIds,
            "Đề tài mới cần duyệt",
            $"Sinh viên đã gửi đề tài \"{project.NameVi}\" cho bạn xem xét.",
            NotificationType.Info,
            NotificationCategory.Project,
            "/lecturer",
            cancellationToken);
    }
}
