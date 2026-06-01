namespace UniThesis.API.Common.Security.Abstractions;

internal interface IAttachmentPromotionService
{
  Task<AttachmentPromotionResult> PromoteAsync(
      string folderPrefix,
      Guid folderPartitionId,
      string quarantinePath,
      CancellationToken cancellationToken = default);
}

internal sealed record AttachmentPromotionResult(bool Success, string? CleanPath, string? ErrorMessage)
{
  public static AttachmentPromotionResult Ok(string cleanPath)
      => new(true, cleanPath, null);

  public static AttachmentPromotionResult Failed(string errorMessage)
      => new(false, null, errorMessage);
}