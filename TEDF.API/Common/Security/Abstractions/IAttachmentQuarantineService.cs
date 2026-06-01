namespace UniThesis.API.Common.Security.Abstractions;

internal interface IAttachmentQuarantineService
{
  Task<QuarantineQueueResult> QueueAsync(
      AttachmentScanContext context,
      IFormFile file,
      CancellationToken cancellationToken = default);
}

internal sealed record QuarantineQueueResult(
    bool Success,
    bool AbortWorkflow,
    string? FilePath,
    string? ErrorMessage = null)
{
  public static QuarantineQueueResult Ok(string filePath) => new(true, false, filePath);

  public static QuarantineQueueResult Failed(string errorMessage, bool abortWorkflow = false)
      => new(false, abortWorkflow, null, errorMessage);
}