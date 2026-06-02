using TEDF.API.Common.Security.Abstractions;

namespace TEDF.API.Common.Security.Services;

internal sealed class AttachmentScanAuditService : IAttachmentScanAuditService
{
  private readonly IMalwareScanAuditLogger _auditLogger;

  public AttachmentScanAuditService(IMalwareScanAuditLogger auditLogger)
  {
    _auditLogger = auditLogger;
  }

  public Task LogQueueUploadFailedAsync(AttachmentScanContext context, IFormFile file, string errorMessage, CancellationToken cancellationToken = default)
      => LogAsync("Error", file, errorMessage, null, BuildMetadata(context, "upload-quarantine"), cancellationToken);

  public Task LogQueuedAsync(AttachmentScanContext context, IFormFile file, string quarantinePath, CancellationToken cancellationToken = default)
      => LogAsync("Queued", file, "Attachment enqueued for asynchronous malware scan.", null, BuildMetadata(context, "queue", ("quarantinePath", quarantinePath)), cancellationToken);

  public Task LogQueueExceptionAsync(AttachmentScanContext context, IFormFile file, Exception exception, CancellationToken cancellationToken = default)
      => LogAsync("Error", file, "Exception while queueing asynchronous malware scan job.", exception.Message, BuildMetadata(context, "queue-exception"), cancellationToken);

  public Task LogRecoveryAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, string cleanPath, CancellationToken cancellationToken = default)
      => LogAsync("Recovery", null, "Recovering Document entity from previously promoted clean file.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "recovery", ("cleanPath", cleanPath)), cancellationToken);

  public Task LogQuarantineMissingAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, CancellationToken cancellationToken = default)
      => LogAsync("Error", null, "Quarantine file not found and no CleanPath recovery available.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "quarantine-missing"), cancellationToken);

  public Task LogUnhandledExceptionAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, Exception exception, CancellationToken cancellationToken = default)
      => LogAsync("Error", null, "Background malware scan job failed with unhandled exception.", exception.ToString(), BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "unhandled-exception"), cancellationToken);

  public Task LogScanUnavailableAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string scanMessage, CancellationToken cancellationToken = default)
      => LogAsync("Warning", file, $"Malware scanner unavailable: {scanMessage}. File kept in quarantine for retry.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "scan-unavailable"), cancellationToken);

  public Task LogQuarantinedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string scanMessage, CancellationToken cancellationToken = default)
      => LogAsync("Quarantined", file, $"Attachment blocked: malware detected — {scanMessage}", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "scan-infected"), cancellationToken);

  public Task LogScanCleanAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, CancellationToken cancellationToken = default)
      => LogAsync("ScanClean", file, "Attachment passed malware scan.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "scan-clean"), cancellationToken);

  public Task LogPromoteFailedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string? promotionError, CancellationToken cancellationToken = default)
      => LogAsync("Error", file, "Failed to promote clean attachment from quarantine to clean storage.", promotionError, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "promote-failed"), cancellationToken);

  public Task LogPromotedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string cleanPath, CancellationToken cancellationToken = default)
      => LogAsync("Promoted", file, "Attachment promoted from quarantine to clean storage.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "promoted", ("cleanPath", cleanPath)), cancellationToken);

  public Task LogCompletedAsync(Guid projectId, Guid uploadedBy, string folderPrefix, string quarantinePath, IFormFile file, string cleanPath, CancellationToken cancellationToken = default)
      => LogAsync("Completed", file, "Attachment scan pipeline completed successfully.", null, BuildMetadata(projectId, uploadedBy, folderPrefix, quarantinePath, "completed", ("cleanPath", cleanPath)), cancellationToken);

  private Task LogAsync(
      string verdict,
      IFormFile? file,
      string message,
      string? scannerResponse,
      Dictionary<string, object?> metadata,
      CancellationToken cancellationToken)
  {
    return _auditLogger.LogAsync(verdict, file, message, scannerResponse, metadata, cancellationToken);
  }

  private static Dictionary<string, object?> BuildMetadata(
      Guid projectId,
      Guid uploadedBy,
      string folderPrefix,
      string quarantinePath,
      string stage,
      params (string Key, object? Value)[] extras)
  {
    var metadata = new Dictionary<string, object?>
    {
      ["projectId"] = projectId,
      ["uploadedBy"] = uploadedBy,
      ["category"] = folderPrefix,
      ["quarantinePath"] = quarantinePath,
      ["stage"] = stage
    };

    foreach (var (key, value) in extras)
    {
      metadata[key] = value;
    }

    return metadata;
  }

  private static Dictionary<string, object?> BuildMetadata(
      AttachmentScanContext context,
      string stage,
      params (string Key, object? Value)[] extras)
  {
    var metadata = new Dictionary<string, object?>
    {
      ["projectId"] = context.ProjectId,
      ["uploadedBy"] = context.UploadedBy,
      ["category"] = context.FolderPrefix,
      ["stage"] = stage
    };

    if (context.ExtraMetadata is not null)
    {
      foreach (var kv in context.ExtraMetadata)
      {
        metadata[kv.Key] = kv.Value;
      }
    }

    foreach (var (key, value) in extras)
    {
      metadata[key] = value;
    }

    return metadata;
  }
}