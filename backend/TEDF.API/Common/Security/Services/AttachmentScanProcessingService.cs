using TEDF.API.Common.Security.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Document;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;

namespace TEDF.API.Common.Security.Services;

internal sealed class AttachmentScanProcessingService : IAttachmentScanProcessingService
{
  private readonly IFileStorageService _fileStorageService;
  private readonly IProjectDocumentWriteService _projectDocumentWriteService;
  private readonly IMalwareScanner _malwareScanner;
  private readonly IAttachmentScanAuditService _auditService;
  private readonly IAttachmentScanNotificationService _notificationService;
  private readonly IAttachmentPromotionService _promotionService;
  private readonly IQuarantinedAttachmentRepository _quarantineTracking;
  private readonly ILogger<AttachmentScanProcessingService> _logger;

  public AttachmentScanProcessingService(
      IFileStorageService fileStorageService,
      IProjectDocumentWriteService projectDocumentWriteService,
      IMalwareScanner malwareScanner,
      IAttachmentScanAuditService auditService,
      IAttachmentScanNotificationService notificationService,
      IAttachmentPromotionService promotionService,
      IQuarantinedAttachmentRepository quarantineTracking,
      ILogger<AttachmentScanProcessingService> logger)
  {
    _fileStorageService = fileStorageService;
    _projectDocumentWriteService = projectDocumentWriteService;
    _malwareScanner = malwareScanner;
    _auditService = auditService;
    _notificationService = notificationService;
    _promotionService = promotionService;
    _quarantineTracking = quarantineTracking;
    _logger = logger;
  }

  public async Task ExecuteAsync(
      string folderPrefix,
      Guid projectId,
      Guid uploadedBy,
      int documentTypeInt,
      Guid folderPartitionId,
      string quarantinePath,
      string originalFileName,
      CancellationToken cancellationToken = default)
  {
    var documentType = (DocumentType)documentTypeInt;

    try
    {
      await using var downloaded = await _fileStorageService.DownloadAsync(quarantinePath, cancellationToken);
      if (downloaded is null)
      {
        var tracking = await _quarantineTracking.GetByQuarantinePathAsync(quarantinePath, cancellationToken);
        if (tracking?.CleanPath is { Length: > 0 } cleanPath)
        {
          _logger.LogInformation(
              "Quarantine file already promoted. Recovering Document entity from CleanPath: {CleanPath}",
              cleanPath);

          await _auditService.LogRecoveryAsync(projectId, uploadedBy, folderPrefix, quarantinePath, cleanPath, cancellationToken);

          var recovered = await _projectDocumentWriteService.InsertDocumentAsync(
              projectId,
              fileName: Path.GetFileName(cleanPath),
              originalFileName: originalFileName,
              fileType: Path.GetExtension(originalFileName).ToLowerInvariant(),
              fileSize: 0,
              filePath: cleanPath,
              documentType: documentType,
              uploadedBy: uploadedBy,
              cancellationToken: cancellationToken);

          if (recovered)
          {
            await _quarantineTracking.DeleteByQuarantinePathAsync(quarantinePath);
            await _notificationService.NotifySuccessAsync(uploadedBy, originalFileName, cancellationToken);
          }
          return;
        }

        await _auditService.LogQuarantineMissingAsync(projectId, uploadedBy, folderPrefix, quarantinePath, cancellationToken);
        await _quarantineTracking.DeleteByQuarantinePathAsync(quarantinePath);
        await _notificationService.NotifyFailureAsync(uploadedBy,
            $"File '{originalFileName}' không tìm thấy trong khu vực quarantine.");
        return;
      }

      Stream scanStream = downloaded;
      if (!downloaded.CanSeek)
      {
        var mem = new MemoryStream();
        await downloaded.CopyToAsync(mem);
        mem.Position = 0;
        downloaded.Dispose();
        scanStream = mem;
      }
      else
      {
        downloaded.Position = 0;
      }

      await ProcessStreamAsync(
          folderPrefix, projectId, uploadedBy, documentType,
          folderPartitionId, quarantinePath, originalFileName,
          scanStream, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Background malware scan job failed for {QuarantinePath}", quarantinePath);

      await _auditService.LogUnhandledExceptionAsync(projectId, uploadedBy, folderPrefix, quarantinePath, ex, cancellationToken);

      await _notificationService.NotifyFailureAsync(uploadedBy,
          $"File '{originalFileName}' gặp lỗi trong quá trình xử lý. Hệ thống sẽ tự động thử lại.");

      throw;
    }
  }

  private async Task ProcessStreamAsync(
      string folderPrefix,
      Guid projectId,
      Guid uploadedBy,
      DocumentType documentType,
      Guid folderPartitionId,
      string quarantinePath,
      string originalFileName,
      Stream stream,
      CancellationToken cancellationToken)
  {
    var length = stream.CanSeek ? stream.Length : 0;
    var formFile = new FormFile(stream, 0, length, "attachments", originalFileName)
    {
      Headers = new HeaderDictionary(),
      ContentType = "application/octet-stream"
    };

    var scanResult = await _malwareScanner.ScanAsync([formFile]);

    if (scanResult.ScannerUnavailable)
    {
      await _auditService.LogScanUnavailableAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, scanResult.Message, cancellationToken);

      throw new InvalidOperationException(
          $"Malware scanner unavailable for '{originalFileName}': {scanResult.Message}");
    }

    if (!scanResult.IsClean)
    {
      await _auditService.LogQuarantinedAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, scanResult.Message, cancellationToken);
      await _quarantineTracking.DeleteByQuarantinePathAsync(quarantinePath);
      await _notificationService.NotifyFailureAsync(uploadedBy,
          $"File '{originalFileName}' bị phát hiện chứa mã độc và đã bị giữ lại.");
      return;
    }

    await _auditService.LogScanCleanAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, cancellationToken);

    var promotionResult = await _promotionService.PromoteAsync(
        folderPrefix,
        folderPartitionId,
        quarantinePath,
        cancellationToken);

    if (!promotionResult.Success || string.IsNullOrWhiteSpace(promotionResult.CleanPath))
    {
      await _auditService.LogPromoteFailedAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, promotionResult.ErrorMessage, cancellationToken);

      throw new InvalidOperationException(
          $"Failed to promote '{originalFileName}' from quarantine: {promotionResult.ErrorMessage}");
    }

    var cleanPath = promotionResult.CleanPath;

    await _auditService.LogPromotedAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, cleanPath, cancellationToken);

    await _quarantineTracking.SetCleanPathAsync(quarantinePath, cleanPath);

    var saved = await _projectDocumentWriteService.InsertDocumentAsync(
        projectId,
        fileName: Path.GetFileName(cleanPath),
        originalFileName: originalFileName,
        fileType: Path.GetExtension(originalFileName).ToLowerInvariant(),
        fileSize: formFile.Length,
        filePath: cleanPath,
        documentType: documentType,
        uploadedBy: uploadedBy,
        cancellationToken: cancellationToken);

    if (!saved)
    {
      return;
    }

    await _quarantineTracking.DeleteByQuarantinePathAsync(quarantinePath);

    await _auditService.LogCompletedAsync(projectId, uploadedBy, folderPrefix, quarantinePath, formFile, cleanPath, cancellationToken);

    await _notificationService.NotifySuccessAsync(uploadedBy, originalFileName, cancellationToken);
  }
}