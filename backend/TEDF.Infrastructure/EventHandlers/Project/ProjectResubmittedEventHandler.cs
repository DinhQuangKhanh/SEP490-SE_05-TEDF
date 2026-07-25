using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Notification;
using TEDF.Infrastructure.Caching;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectResubmittedEventHandler : INotificationHandler<ProjectResubmittedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectResubmittedEventHandler> _logger;

        public ProjectResubmittedEventHandler(
            INotificationService notificationService,
            IProjectEvaluatorAssignmentRepository assignmentRepository,
            IProjectRepository projectRepository,
            ICacheInvalidationService cacheInvalidation,
            IUnitOfWork unitOfWork,
            ILogger<ProjectResubmittedEventHandler> logger)
        {
            _notificationService = notificationService;
            _assignmentRepository = assignmentRepository;
            _projectRepository = projectRepository;
            _cacheInvalidation = cacheInvalidation;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ProjectResubmittedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Only reset evaluator assignments on resubmission (not first submission)
                if (notification.SubmissionNumber <= 1) return;

                var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(
                    notification.ProjectId, cancellationToken)).ToList();

                if (assignments.Count == 0) return;

                // Reset all active evaluator assignments
                foreach (var assignment in assignments)
                {
                    assignment.ResetEvaluation();
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Send notification to evaluators
                var project = await _projectRepository.GetByIdAsync(notification.ProjectId, cancellationToken);
                var projectName = project?.NameVi.Value ?? "Không xác định";

                var evaluatorIds = assignments.Select(a => a.EvaluatorId).ToList();
                await _notificationService.SendToMultipleAsync(
                    evaluatorIds,
                    "Yêu cầu thẩm định lại đề tài",
                    $"Đề tài \"{projectName}\" đã được nộp lại để thẩm định lần {notification.SubmissionNumber}. Vui lòng thẩm định lại.",
                    NotificationType.Info,
                    NotificationCategory.Evaluation,
                    ct: cancellationToken);

                // Invalidate evaluator caches
                foreach (var assignment in assignments)
                {
                    await _cacheInvalidation.InvalidateEvaluatorCacheAsync(assignment.EvaluatorId, cancellationToken);
                }

                _logger.LogInformation(
                    "Project {ProjectId} resubmitted (submission #{Number}). Reset {Count} evaluator assignments.",
                    notification.ProjectId, notification.SubmissionNumber, assignments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Project {ProjectId}",
                    nameof(ProjectResubmittedEvent), notification.ProjectId);
            }
        }
    }
}
