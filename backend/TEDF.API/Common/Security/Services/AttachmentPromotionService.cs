using TEDF.API.Common.Security.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.API.Common.Security.Services;

internal sealed class AttachmentPromotionService : IAttachmentPromotionService
{
  private readonly IFileStorageService _fileStorageService;
  private readonly IQuarantinedAttachmentRepository _quarantineTracking;

  public AttachmentPromotionService(
      IFileStorageService fileStorageService,
      IQuarantinedAttachmentRepository quarantineTracking)
  {
    _fileStorageService = fileStorageService;
    _quarantineTracking = quarantineTracking;
  }

  public async Task<AttachmentPromotionResult> PromoteAsync(
      string folderPrefix,
      Guid folderPartitionId,
      string quarantinePath,
      CancellationToken cancellationToken = default)
  {
    var cleanFolder = $"{folderPrefix}/clean/{folderPartitionId:N}/{DateTime.UtcNow:yyyyMMdd}";
    var moveResult = await _fileStorageService.MoveAsync(quarantinePath, cleanFolder);

    if (!moveResult.Success || string.IsNullOrWhiteSpace(moveResult.FilePath))
    {
      return AttachmentPromotionResult.Failed(moveResult.Error ?? "Unknown promotion error.");
    }

    var cleanPath = moveResult.FilePath;
    await _quarantineTracking.SetCleanPathAsync(quarantinePath, cleanPath);

    return AttachmentPromotionResult.Ok(cleanPath);
  }
}