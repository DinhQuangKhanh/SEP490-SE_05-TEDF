using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Notification;

namespace TEDF.Infrastructure.EventHandlers.Semesters
{
    /// <summary>
    /// On roster publish: notifies mentors assigned next semester (with their program), and mentors
    /// supervising the current semester but NOT assigned next (access will expire).
    /// </summary>
    public class NotifyMentorsOnRosterPublishedHandler : INotificationHandler<SemesterRosterPublishedEvent>
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IMajorReadRepository _majorRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotifyMentorsOnRosterPublishedHandler> _logger;

        public NotifyMentorsOnRosterPublishedHandler(
            ISemesterRepository semesterRepository,
            IProjectRepository projectRepository,
            IMajorReadRepository majorRepository,
            INotificationService notificationService,
            ILogger<NotifyMentorsOnRosterPublishedHandler> logger)
        {
            _semesterRepository = semesterRepository;
            _projectRepository = projectRepository;
            _majorRepository = majorRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(SemesterRosterPublishedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var semester = await _semesterRepository.GetWithRosterAsync(notification.SemesterId, cancellationToken);
                if (semester is null) return;

                var assigned = semester.EligibleMentors.Where(m => m.IsAssigned).ToList();
                var assignedIds = assigned.Select(m => m.MentorId).ToHashSet();

                var majorNames = (await _majorRepository.GetAllAsync(cancellationToken))
                    .ToDictionary(m => m.Id, m => m.Name);

                // Assigned mentors → personalized message with their program (Major) name.
                foreach (var mentor in assigned)
                {
                    var majorName = mentor.MajorId is int mid && majorNames.TryGetValue(mid, out var name)
                        ? name
                        : "ngành được phân công";
                    await _notificationService.SendAsync(
                        mentor.MentorId,
                        "Phân công hướng dẫn ĐATN học kỳ tới",
                        $"Bạn đã được phân công làm giảng viên hướng dẫn đồ án tốt nghiệp ngành {majorName} trong học kỳ tới.",
                        NotificationType.Success,
                        NotificationCategory.System,
                        ct: cancellationToken);
                }

                // Mentors supervising the current semester but not assigned next → access-expiry notice.
                var current = await _semesterRepository.GetActiveAsync(cancellationToken);
                if (current is not null)
                {
                    var currentMentorIds = await _projectRepository.GetActiveMentorIdsInSemesterAsync(current.Id, cancellationToken);
                    var notAssigned = currentMentorIds.Where(id => !assignedIds.Contains(id)).Distinct().ToList();
                    if (notAssigned.Count > 0)
                    {
                        await _notificationService.SendToMultipleAsync(
                            notAssigned,
                            "Kết thúc phân công hướng dẫn",
                            "Bạn chưa được phân công hướng dẫn đồ án tốt nghiệp trong học kỳ tới. " +
                            "Quyền truy cập hệ thống với vai trò giảng viên hướng dẫn của bạn sẽ hết hạn sau khi học kỳ hiện tại kết thúc.",
                            NotificationType.Warning,
                            NotificationCategory.System,
                            ct: cancellationToken);
                    }
                }

                _logger.LogInformation(
                    "Roster-published mentor notifications sent for Semester {SemesterId}: {Assigned} assigned.",
                    notification.SemesterId, assigned.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {Event} for Semester {SemesterId}",
                    nameof(SemesterRosterPublishedEvent), notification.SemesterId);
            }
        }
    }
}
