namespace UniThesis.API.Common.Security.Abstractions;

internal interface IAttachmentScanAuditService
{
  Task LogQueueUploadFailedAsync(AttachmentScanContext context, IFormFile file, string errorMessage, CancellationToken cancellationToken = default);

  Task LogQueuedAsync(AttachmentScanContext context, IFormFile file, string quarantinePath, CancellationToken cancellationToken = default);

  Task LogQueueExceptionAsync(AttachmentScanContext context, IFormFile file, Exception exception, CancellationToken cancellationToken = default);

  Task LogRecoveryAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, string cleanPath, CancellationToken cancellationToken = default);

  Task LogQuarantineMissingAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, CancellationToken cancellationToken = default);

  Task LogUnhandledExceptionAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, Exception exception, CancellationToken cancellationToken = default);

  Task LogScanUnavailableAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string scanMessage, CancellationToken cancellationToken = default);

  Task LogQuarantinedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string scanMessage, CancellationToken cancellationToken = default);

  Task LogScanCleanAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, CancellationToken cancellationToken = default);

  Task LogPromoteFailedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string? promotionError, CancellationToken cancellationToken = default);

  Task LogPromotedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string cleanPath, CancellationToken cancellationToken = default);

  Task LogCompletedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string cleanPath, CancellationToken cancellationToken = default);
}