using TEDF.API.Common.Security.Abstractions;

namespace TEDF.API.Common.Security.Jobs;

/// <summary>
/// Background job: download from quarantine → malware scan → promote to clean folder → create Document entity.
/// Parameterised by folder prefix + DocumentType so a single class handles all upload contexts.
/// </summary>
internal sealed class AttachmentScanJob
{
  private readonly IAttachmentScanProcessingService _processingService;

  public AttachmentScanJob(IAttachmentScanProcessingService processingService)
  {
    _processingService = processingService;
  }

  /// <param name="folderPrefix">Storage category, e.g. "topic-proposals" or "topic-documents".</param>
  /// <param name="documentTypeInt">Serialised <see cref="DocumentType"/> enum value.</param>
  /// <param name="folderPartitionId">ID used to partition clean storage (poolId for proposals, projectId for documents).</param>
  public Task ExecuteAsync(
      string folderPrefix,
      Guid projectId,
      Guid uploadedBy,
      int documentTypeInt,
      Guid folderPartitionId,
      string quarantinePath,
      string originalFileName)
      => _processingService.ExecuteAsync(
          folderPrefix,
          projectId,
          uploadedBy,
          documentTypeInt,
          folderPartitionId,
          quarantinePath,
          originalFileName);
}