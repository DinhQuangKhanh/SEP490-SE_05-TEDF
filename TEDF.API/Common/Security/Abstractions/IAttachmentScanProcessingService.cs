namespace UniThesis.API.Common.Security.Abstractions;

internal interface IAttachmentScanProcessingService
{
  Task ExecuteAsync(
      string folderPrefix,
      Guid projectId,
      Guid uploadedBy,
      int documentTypeInt,
      Guid folderPartitionId,
      string quarantinePath,
      string originalFileName,
      CancellationToken cancellationToken = default);
}