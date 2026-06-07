using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.DirectTopic;

public class ProjectMentorApprovedEventHandler : INotificationHandler<ProjectMentorApprovedEvent>
{
    private readonly ILogger<ProjectMentorApprovedEventHandler> _logger;
    private readonly INotificationService _notificationService;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectMentorApprovedEventHandler(
        ILogger<ProjectMentorApprovedEventHandler> logger,
        INotificationService notificationService,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _notificationService = notificationService;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ProjectMentorApprovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Project {ProjectId} approved by mentor {MentorId}",
            notification.ProjectId, notification.MentorId);

        var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project?.GroupId is null) return;

        var group = await _groupRepository.GetWithMembersAsync(project.GroupId.Value, cancellationToken);
        if (group?.LeaderId is null) return;

        await _notificationService.SendAsync(
            group.LeaderId.Value,
            "Giảng viên đã duyệt đề tài",
            $"Đề tài \"{project.NameVi}\" đã được giảng viên duyệt và gửi đi thẩm định.",
            NotificationType.Success,
            NotificationCategory.Project,
            $"/student/my-topic",
            cancellationToken);

        // On resubmission (EvaluationCount > 1): reset evaluator results and notify evaluators
        if (project.EvaluationCount > 1)
        {
            var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(
                notification.ProjectId, cancellationToken)).ToList();

            if (assignments.Count > 0)
            {
                foreach (var assignment in assignments)
                {
                    assignment.ResetEvaluation();
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var evaluatorIds = assignments.Select(a => a.EvaluatorId).ToList();
                await _notificationService.SendToMultipleAsync(
                    evaluatorIds,
                    "Yêu cầu thẩm định lại đề tài",
                    $"Đề tài \"{project.NameVi}\" đã được nộp lại để thẩm định lần {project.EvaluationCount}. Vui lòng thẩm định lại.",
                    NotificationType.Info,
                    NotificationCategory.Evaluation,
                    ct: cancellationToken);

                _logger.LogInformation(
                    "Project {ProjectId} resubmitted (evaluation #{Count}). Reset {AssignmentCount} evaluator assignments.",
                    notification.ProjectId, project.EvaluationCount, assignments.Count);
            }
        }
    }
}
