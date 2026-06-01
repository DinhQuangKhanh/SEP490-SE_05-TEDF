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
using TEDF.Persistence.MongoDB.Documents;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Evaluation
{
    public class EvaluatorSubmittedResultEventHandler : INotificationHandler<EvaluatorSubmittedResultEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IEvaluationLogRepository _evaluationLogRepository;
        private readonly ILogger<EvaluatorSubmittedResultEventHandler> _logger;

        public EvaluatorSubmittedResultEventHandler(
            INotificationService notificationService,
            IProjectRepository projectRepository,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            IUserRepository userRepository,
            IDepartmentRepository departmentRepository,
            IEvaluationLogRepository evaluationLogRepository,
            ILogger<EvaluatorSubmittedResultEventHandler> logger)
        {
            _notificationService = notificationService;
            _projectRepository = projectRepository;
            _assignmentRepository = assignmentRepository;
            _userRepository = userRepository;
            _departmentRepository = departmentRepository;
            _evaluationLogRepository = evaluationLogRepository;
            _logger = logger;
        }

        public async Task Handle(EvaluatorSubmittedResultEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Log the evaluation submission
                await _evaluationLogRepository.AddAsync(new EvaluationLogDocument
                {
                    ProjectId = notification.ProjectId,
                    Action = EvaluationAction.Completed,
                    Result = notification.Result,
                    PerformedBy = notification.EvaluatorId,
                    PerformedAt = DateTime.UtcNow
                }, cancellationToken);

                // Load project and assignments
                var project = await _projectRepository.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
                if (project is null) return;

                var projectName = project.NameVi.Value;

                var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(
                    notification.ProjectId, cancellationToken)).ToList();
                var submittedAssignments = assignments.Where(a => a.HasSubmittedEvaluation).ToList();

                // Get mentor IDs
                var mentorIds = project.Mentors.Where(m => m.IsActive).Select(m => m.MentorId).ToList();
                var evaluatorIds = assignments.Select(a => a.EvaluatorId).ToList();

                // Get department head ID
                var departmentHeadId = await GetDepartmentHeadIdAsync(project.MajorId, cancellationToken);

                // Only 1 evaluator submitted so far — no notifications needed
                if (submittedAssignments.Count < 2) return;

                var results = submittedAssignments.Select(a => a.IndividualResult!.Value).Distinct().ToList();
                var mentorName = await GetUserNameAsync(mentorIds.FirstOrDefault(), cancellationToken);

                if (results.Count == 1)
                {
                    // Both evaluators agree
                    var agreedResult = results[0];
                    await HandleAgreedResultAsync(
                        agreedResult, projectName, mentorName, mentorIds, evaluatorIds, departmentHeadId, cancellationToken);
                }
                else
                {
                    // Evaluators disagree — CNBM must decide
                    await HandleConflictingResultsAsync(
                        projectName, mentorIds, evaluatorIds, departmentHeadId, cancellationToken);
                }

                _logger.LogInformation("Evaluation result submitted for project {ProjectId} by {EvaluatorId}: {Result}",
                    notification.ProjectId, notification.EvaluatorId, notification.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(EvaluatorSubmittedResultEvent), notification.ProjectId);
            }
        }

        private async Task HandleAgreedResultAsync(
            EvaluationResult result, string projectName, string mentorName,
            List<Guid> mentorIds, List<Guid> evaluatorIds, Guid? departmentHeadId,
            CancellationToken ct)
        {
            var allRecipients = new List<Guid>();
            allRecipients.AddRange(mentorIds);
            allRecipients.AddRange(evaluatorIds);
            if (departmentHeadId.HasValue) allRecipients.Add(departmentHeadId.Value);
            allRecipients = allRecipients.Distinct().ToList();

            switch (result)
            {
                case EvaluationResult.Approved:
                    await _notificationService.SendToMultipleAsync(
                        allRecipients,
                        "Đề tài được duyệt",
                        $"Đề tài '{projectName}' đã được duyệt bởi cả hai người thẩm định.",
                        NotificationType.Success,
                        NotificationCategory.Evaluation,
                        ct: ct);
                    break;

                case EvaluationResult.NeedsModification:
                    await _notificationService.SendToMultipleAsync(
                        allRecipients,
                        "Đề tài cần chỉnh sửa",
                        $"Đề tài '{projectName}' cần được chỉnh sửa theo yêu cầu của người thẩm định.",
                        NotificationType.Warning,
                        NotificationCategory.Evaluation,
                        ct: ct);
                    break;

                case EvaluationResult.Rejected:
                    await _notificationService.SendToMultipleAsync(
                        allRecipients,
                        "Đề tài bị từ chối",
                        $"Đề tài '{projectName}' do {mentorName} làm Mentor đã bị từ chối, sẽ được xóa khỏi hệ thống trong 5 phút.",
                        NotificationType.Error,
                        NotificationCategory.Evaluation,
                        ct: ct);
                    break;
            }
        }

        private async Task HandleConflictingResultsAsync(
            string projectName, List<Guid> mentorIds, List<Guid> evaluatorIds, Guid? departmentHeadId,
            CancellationToken ct)
        {
            // Notify CNBM — they need to make a final decision
            if (departmentHeadId.HasValue)
            {
                await _notificationService.SendAsync(
                    departmentHeadId.Value,
                    "Cần quyết định thẩm định",
                    $"Kết quả thẩm định đề tài '{projectName}' không thống nhất, vui lòng đưa ra quyết định cuối cùng.",
                    NotificationType.Warning,
                    NotificationCategory.Evaluation,
                    "/department-head/assign",
                    ct);
            }

            // Notify evaluators and mentor
            var otherRecipients = new List<Guid>();
            otherRecipients.AddRange(mentorIds);
            otherRecipients.AddRange(evaluatorIds);
            otherRecipients = otherRecipients.Distinct().ToList();

            await _notificationService.SendToMultipleAsync(
                otherRecipients,
                "Chờ quyết định CNBM",
                $"Kết quả thẩm định đề tài '{projectName}' đang chờ chủ nhiệm bộ môn quyết định.",
                NotificationType.Info,
                NotificationCategory.Evaluation,
                ct: ct);
        }

        private async Task<Guid?> GetDepartmentHeadIdAsync(int majorId, CancellationToken ct)
        {
            var departments = await _departmentRepository.GetAllAsync(ct);
            foreach (var dept in departments)
            {
                if (await _departmentRepository.IsMajorInDepartmentAsync(majorId, dept.Id, ct))
                    return dept.HeadOfDepartmentId;
            }
            return null;
        }

        private async Task<string> GetUserNameAsync(Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty) return "Không xác định";
            var user = await _userRepository.GetByIdAsync(userId, ct);
            return user?.FullName ?? "Không xác định";
        }
    }
}
