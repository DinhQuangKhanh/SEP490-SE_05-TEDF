using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Domain.Enums.Semester;
using TEDF.Persistence.SqlServer.Constants;

namespace TEDF.Infrastructure.EventHandlers.Semester;

public class PhaseUpcomingEventHandler : INotificationHandler<PhaseUpcomingEvent>, IInfrastructureEventHandlerMarker
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PhaseUpcomingEventHandler> _logger;

    public PhaseUpcomingEventHandler(
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<PhaseUpcomingEventHandler> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(PhaseUpcomingEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Phase upcoming event triggered: SemesterId={SemesterId}, PhaseId={PhaseId}, PhaseType={PhaseType}",
            notification.SemesterId,
            notification.PhaseId,
            notification.PhaseType);

        // We only care about the Registration phase
        if (notification.PhaseType != SemesterPhaseType.Registration)
        {
            return;
        }

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
            var title = "Sắp tới hạn đăng ký đề tài/đồ án";
            var content = $"Thời gian đăng ký đề tài cho học kỳ sắp diễn ra (còn 3 ngày). Quý thầy cô và sinh viên vui lòng chuẩn bị.";

            await _notificationService.SendToMultipleAsync(
                userIds: targetUserIds,
                title: title,
                content: content,
                type: NotificationType.Warning,
                category: NotificationCategory.System,
                targetUrl: null,
                ct: cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched 'Phase Upcoming' notifications to {Count} users for SemesterId={SemesterId}, PhaseType={PhaseType}.",
                targetUserIds.Count,
                notification.SemesterId,
                notification.PhaseType);
        }
        else
        {
            _logger.LogWarning("No target users found to notify for upcoming phase Registration in SemesterId={SemesterId}", notification.SemesterId);
        }
    }
}
