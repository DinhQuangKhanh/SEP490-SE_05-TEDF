using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Enums.Notification;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class DepartmentHeadFinalDecisionEventHandler : INotificationHandler<DepartmentHeadFinalDecisionEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ILogger<DepartmentHeadFinalDecisionEventHandler> _logger;

        public DepartmentHeadFinalDecisionEventHandler(
            INotificationService notificationService,
            IProjectRepository projectRepository,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IDepartmentRepository departmentRepository,
            ILogger<DepartmentHeadFinalDecisionEventHandler> logger)
        {
            _notificationService = notificationService;
            _projectRepository = projectRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _departmentRepository = departmentRepository;
            _logger = logger;
        }

        public async Task Handle(DepartmentHeadFinalDecisionEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
                if (project is null) return;

                var projectName = project.NameVi.Value;
                var mentorIds = project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId).ToList();
                var mentorName = "Không xác định";
                if (mentorIds.Count > 0)
                {
                    var mentor = await _userRepository.GetByIdAsync(mentorIds[0], cancellationToken);
                    mentorName = mentor?.FullName ?? mentorName;
                }

                var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(
                    notification.ProjectId, cancellationToken)).ToList();
                var evaluatorIds = assignments.Select(a => a.EvaluatorId).ToList();

                // All recipients: mentor + evaluators
                var allRecipients = new List<Guid>();
                allRecipients.AddRange(mentorIds);
                allRecipients.AddRange(evaluatorIds);
                allRecipients = allRecipients.Distinct().ToList();

                switch (notification.FinalResult)
                {
                    case EvaluationResult.Approved:
                        await _notificationService.SendToMultipleAsync(
                            allRecipients,
                            "CNBM duyệt đề tài",
                            $"Chủ nhiệm bộ môn đã quyết định duyệt đề tài '{projectName}'.",
                            NotificationType.Success,
                            NotificationCategory.Evaluation,
                            ct: cancellationToken);
                        break;

                    case EvaluationResult.NeedsModification:
                        await _notificationService.SendToMultipleAsync(
                            allRecipients,
                            "CNBM yêu cầu chỉnh sửa",
                            $"Chủ nhiệm bộ môn đã yêu cầu chỉnh sửa đề tài '{projectName}'.",
                            NotificationType.Warning,
                            NotificationCategory.Evaluation,
                            ct: cancellationToken);
                        break;

                    case EvaluationResult.Rejected:
                        await _notificationService.SendToMultipleAsync(
                            allRecipients,
                            "Đề tài bị từ chối",
                            $"Đề tài '{projectName}' do {mentorName} làm Mentor đã bị từ chối, sẽ được xóa khỏi hệ thống trong 5 phút.",
                            NotificationType.Error,
                            NotificationCategory.Evaluation,
                            ct: cancellationToken);
                        break;
                }

                _logger.LogInformation(
                    "Department Head final decision for project {ProjectId}: {Result}",
                    notification.ProjectId, notification.FinalResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(DepartmentHeadFinalDecisionEvent), notification.ProjectId);
            }
        }
    }
}
