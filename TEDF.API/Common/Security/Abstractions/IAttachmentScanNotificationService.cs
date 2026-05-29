namespace UniThesis.API.Common.Security.Abstractions;

internal interface IAttachmentScanNotificationService
{
  Task NotifySuccessAsync(Guid userId, string fileName, CancellationToken cancellationToken = default);

  Task NotifyFailureAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}