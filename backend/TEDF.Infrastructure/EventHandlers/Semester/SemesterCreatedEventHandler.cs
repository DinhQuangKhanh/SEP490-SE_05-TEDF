using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Enums.Notification;
using TEDF.Persistence.SqlServer.Constants;

namespace TEDF.Infrastructure.EventHandlers.Semester;

public class SemesterCreatedEventHandler : INotificationHandler<SemesterCreatedEvent>, IInfrastructureEventHandlerMarker
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SemesterCreatedEventHandler> _logger;

    public SemesterCreatedEventHandler(
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<SemesterCreatedEventHandler> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(SemesterCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Semester created: Id={SemesterId}, Code={SemesterCode}. Initializing system notifications...",
            notification.SemesterId,
            notification.SemesterCode);

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
            var title = "Học kỳ mới được tạo";
            var content = $"Học kỳ mới {notification.SemesterCode} đã được khởi tạo trong hệ thống.";

            await _notificationService.SendToMultipleAsync(
                userIds: targetUserIds,
                title: title,
                content: content,
                type: NotificationType.Info,
                category: NotificationCategory.System,
                targetUrl: null, // Depending on requirements, we could link to a semester view
                ct: cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched 'Semester Created' notifications to {Count} users for SemesterCode={SemesterCode}.",
                targetUserIds.Count,
                notification.SemesterCode);
        }
        else
        {
            _logger.LogWarning("No target users found to notify for SemesterCode={SemesterCode}", notification.SemesterCode);
        }
    }
}
