using UniThesis.API.Common.Security.Abstractions;
using UniThesis.Application.Common.Interfaces;
using UniThesis.Domain.Enums.Notification;

namespace UniThesis.API.Common.Security.Services;

internal sealed class AttachmentScanNotificationService : IAttachmentScanNotificationService
{
  private readonly INotificationService _notificationService;
  private readonly ILogger<AttachmentScanNotificationService> _logger;

  public AttachmentScanNotificationService(
      INotificationService notificationService,
      ILogger<AttachmentScanNotificationService> logger)
  {
    _notificationService = notificationService;
    _logger = logger;
  }

  public async Task NotifySuccessAsync(Guid userId, string fileName, CancellationToken cancellationToken = default)
  {
    try
    {
      await _notificationService.SendAsync(
          userId: userId,
          title: "Tải lên tài liệu thành công",
          content: $"File '{fileName}' đã được quét sạch và lưu vào hệ thống.",
          type: NotificationType.Success,
          category: NotificationCategory.Project);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to send success notification to user {UserId}", userId);
    }
  }

  public async Task NotifyFailureAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
  {
    try
    {
      await _notificationService.SendAsync(
          userId: userId,
          title: "Tải lên tài liệu thất bại",
          content: reason,
          type: NotificationType.Error,
          category: NotificationCategory.Project);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to send failure notification to user {UserId}", userId);
    }
  }
}