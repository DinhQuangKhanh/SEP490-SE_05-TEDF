using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Notification;
using TEDF.Domain.Enums.Project;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    public class ProjectCreatedEventHandler : INotificationHandler<ProjectCreatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly IProjectRepository _projectRepo;
        private readonly IUserRepository _userRepo;
        private readonly IMajorReadRepository _majorRepo;
        private readonly IDepartmentRepository _departmentRepo;
        private readonly ILogger<ProjectCreatedEventHandler> _logger;

        public ProjectCreatedEventHandler(
            INotificationService notificationService,
            IProjectRepository projectRepo,
            IUserRepository userRepo,
            IMajorReadRepository majorRepo,
            IDepartmentRepository departmentRepo,
            ILogger<ProjectCreatedEventHandler> logger)
        {
            _notificationService = notificationService;
            _projectRepo = projectRepo;
            _userRepo = userRepo;
            _majorRepo = majorRepo;
            _departmentRepo = departmentRepo;
            _logger = logger;
        }

        public async Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Project created: {ProjectId}, Code: {ProjectCode}",
                notification.ProjectId, notification.ProjectCode);

            // Only notify department head for pool-sourced projects
            if (notification.SourceType != ProjectSourceType.FromPool)
                return;

            try
            {
                // 1. Load project with mentors
                var project = await _projectRepo.GetWithMentorsAsync(notification.ProjectId, cancellationToken);
                if (project is null) return;

                // 2. Get first active mentor's name
                var mentorId = project.Mentors.FirstOrDefault(m => m.IsActive)?.MentorId;
                if (mentorId is null) return;

                var mentor = await _userRepo.GetByIdAsync(mentorId.Value, cancellationToken);
                var mentorName = mentor?.FullName ?? "Một giảng viên";

                // 3. Trace: MajorId → Major.DepartmentId → Department.HeadOfDepartmentId
                var major = await _majorRepo.GetByIdAsync(project.MajorId, cancellationToken);
                if (major is null) return;

                var department = await _departmentRepo.GetByIdAsync(major.DepartmentId, cancellationToken);
                if (department?.HeadOfDepartmentId is null) return;

                // 4. Send notification to department head
                await _notificationService.SendAsync(
                    department.HeadOfDepartmentId.Value,
                    "Đề xuất đề tài mới",
                    $"Giảng viên {mentorName} đã đề xuất một đề tài mới: {project.NameVi.Value}.",
                    NotificationType.Info,
                    NotificationCategory.TopicPool,
                    "/lecturer/create",
                    cancellationToken);

                _logger.LogInformation(
                    "Notification sent to department head {DeptHeadId} for project {ProjectId}",
                    department.HeadOfDepartmentId.Value, notification.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending notification for ProjectCreatedEvent {ProjectId}",
                    notification.ProjectId);
            }
        }
    }
}
