using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Domain.Enums.Semester;
using TEDF.Persistence.SqlServer.Constants;

namespace TEDF.Infrastructure.EventHandlers.Semester;

public class PhaseStartedEventHandler : INotificationHandler<PhaseStartedEvent>, IInfrastructureEventHandlerMarker
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PhaseStartedEventHandler> _logger;

    public PhaseStartedEventHandler(
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<PhaseStartedEventHandler> logger)
    {
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(PhaseStartedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.PhaseType != SemesterPhaseType.Registration)
        {
            // We only care about the Registration phase starting for this specific notification feature.
            return;
        }

        _logger.LogInformation(
            "Topic Registration phase started for SemesterId={SemesterId}. Initializing system notifications...",
            notification.SemesterId);

        var semester = await _semesterRepository.GetByIdAsync(notification.SemesterId, cancellationToken);
        var semesterCodeStr = semester != null ? semester.Code.Value : notification.SemesterId.ToString();

        // Get all students
        var students = await _userRepository.GetByRoleAsync(RoleNames.Student, cancellationToken);
        
        // Get lecturers (interpreting this as Mentor, Evaluator, DepartmentHead based on the available roles)
        var mentors = await _userRepository.GetByRoleAsync(RoleNames.Mentor, cancellationToken);
        var evaluators = await _userRepository.GetByRoleAsync(RoleNames.Evaluator, cancellationToken);
        var departmentHeads = await _userRepository.GetByRoleAsync(RoleNames.DepartmentHead, cancellationToken);

        // Combine unique user IDs
        var targetUserIds = students.Select(u => u.Id)
            .Union(mentors.Select(u => u.Id))
            .Union(evaluators.Select(u => u.Id))
            .Union(departmentHeads.Select(u => u.Id))
            .Distinct()
            .ToList();

        if (targetUserIds.Count > 0)
        {
            var title = "Thời gian đăng ký đề tài";
            var content = $"Thời gian đăng ký đề tài trong học kỳ {semesterCodeStr} đã chính thức bắt đầu, vui lòng truy cập hệ thống để thực hiện các thao tác quản lý và đăng ký hoặc theo dõi tiến độ công việc.";

            await _notificationService.SendToMultipleAsync(
                userIds: targetUserIds,
                title: title,
                content: content,
                type: NotificationType.Info,
                category: NotificationCategory.System,
                targetUrl: "/", // Broadcast to mixed roles — "/" lets RoleBasedRedirect send each user to their own home
                ct: cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched 'Topic Registration Phase Started' notifications to {Count} users for SemesterCode={SemesterCode}.",
                targetUserIds.Count,
                semesterCodeStr);
        }
        else
        {
            _logger.LogWarning("No target users found to notify for Semester Registration Phase (Code={SemesterCode})", semesterCodeStr);
        }
    }
}
