using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Notification;
using TEDF.Persistence.SqlServer;

namespace TEDF.Infrastructure.BackgroundJobs.Jobs
{
    public class GroupJoinRequestExpirationJob
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<GroupJoinRequestExpirationJob> _logger;

        public GroupJoinRequestExpirationJob(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<GroupJoinRequestExpirationJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.UtcNow;

            var expired = await (
                from request in _context.GroupJoinRequests
                where request.Status == GroupJoinRequestStatus.Pending
                   && request.ExpiresAt <= now
                join grp in _context.Groups on request.GroupId equals grp.Id
                select new { request, grp }
            ).ToListAsync();

            if (expired.Count == 0)
            {
                _logger.LogInformation("GroupJoinRequestExpirationJob completed: no expired requests.");
                return;
            }

            foreach (var item in expired)
            {
                item.request.Reject();
            }

            await _context.SaveChangesAsync();

            foreach (var item in expired)
            {
                await _notificationService.SendAsync(
                    item.request.StudentId,
                    "Yêu cầu tham gia đã hết hạn",
                    $"Yêu cầu tham gia nhóm {item.grp.Code} đã tự động bị từ chối vì quá 1 giờ chưa được phản hồi.",
                    NotificationType.Warning,
                    NotificationCategory.Group,
                    "/student/open-groups");
            }

            _logger.LogInformation("GroupJoinRequestExpirationJob completed: auto-rejected {Count} request(s).", expired.Count);
        }
    }
}
