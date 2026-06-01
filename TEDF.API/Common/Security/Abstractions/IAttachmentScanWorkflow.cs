using UniThesis.Domain.Enums.Document;

namespace UniThesis.API.Common.Security.Abstractions;

/// <summary>
/// Captures what varies between upload contexts (topic proposals vs topic documents, etc.).
/// Passed to <see cref="IAttachmentScanWorkflow.QueueAsync"/> so a single workflow
/// implementation handles all upload scenarios.
/// </summary>
internal sealed record AttachmentScanContext(
    string FolderPrefix,
    Guid ProjectId,
    Guid UploadedBy,
    Guid FolderPartitionId,
    DocumentType DocumentType,
    IDictionary<string, object?>? ExtraMetadata = null);

/// <summary>
/// Unified quarantine → malware-scan → promote workflow for any file upload.
/// Replaces the duplicate ITopicProposalAttachmentScanWorkflow / ITopicDocumentScanWorkflow.
/// </summary>
internal interface IAttachmentScanWorkflow
{
  Task<AttachmentQueueResult> QueueAsync(
      AttachmentScanContext context,
      IReadOnlyCollection<IFormFile>? attachments,
      CancellationToken cancellationToken = default);
}

internal sealed record AttachmentQueueResult(
    bool Success,
    int QueuedCount,
    string? ErrorMessage = null)
{
  public static AttachmentQueueResult Ok(int queuedCount) => new(true, queuedCount);

  public static AttachmentQueueResult Failed(string errorMessage, int queuedCount = 0)
      => new(false, queuedCount, errorMessage);
}