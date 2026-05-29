using Microsoft.Extensions.Options;
using UniThesis.API.Common.Security.Abstractions;
using UniThesis.API.Common.Security.Jobs;
using UniThesis.API.Common.Security.Options;
using UniThesis.Application.Common.Interfaces;
using UniThesis.Persistence.MongoDB.Documents;
using UniThesis.Persistence.MongoDB.Repositories.Interfaces;

namespace UniThesis.API.Common.Security.Services;

internal sealed class AttachmentQuarantineService : IAttachmentQuarantineService
{
  private readonly IFileStorageService _fileStorageService;
  private readonly IBackgroundJobService _backgroundJobService;
  private readonly IAttachmentScanAuditService _auditService;
  private readonly IQuarantinedAttachmentRepository _quarantineTracking;
  private readonly MalwareScanOptions _scanOptions;
  private readonly ILogger<AttachmentQuarantineService> _logger;

  public AttachmentQuarantineService(
      IFileStorageService fileStorageService,
      IBackgroundJobService backgroundJobService,
      IAttachmentScanAuditService auditService,
      IQuarantinedAttachmentRepository quarantineTracking,
      IOptions<MalwareScanOptions> scanOptions,
      ILogger<AttachmentQuarantineService> logger)
  {
    _fileStorageService = fileStorageService;
    _backgroundJobService = backgroundJobService;
    _auditService = auditService;
    _quarantineTracking = quarantineTracking;
    _scanOptions = scanOptions.Value;
    _logger = logger;
  }

  public async Task<QuarantineQueueResult> QueueAsync(
      AttachmentScanContext context,
      IFormFile file,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var quarantineFolder = $"{context.FolderPrefix}/quarantine/{context.FolderPartitionId:N}/{DateTime.UtcNow:yyyyMMdd}";

      await using var stream = file.OpenReadStream();
      var uploadResult = await _fileStorageService.UploadAsync(
          stream,
          file.FileName,
          quarantineFolder,
          cancellationToken);

      if (!uploadResult.Success || string.IsNullOrWhiteSpace(uploadResult.FilePath))
      {
        var error = uploadResult.Error ?? "Không thể lưu file vào khu vực quarantine.";
        await _auditService.LogQueueUploadFailedAsync(context, file, error, cancellationToken);

        return QuarantineQueueResult.Failed(
            _scanOptions.FailClosed
                ? "Không thể đưa tệp đính kèm vào hàng đợi quét mã độc. Vui lòng thử lại sau."
                : error,
            abortWorkflow: _scanOptions.FailClosed);
      }

      var quarantinePath = uploadResult.FilePath;
      var documentTypeInt = (int)context.DocumentType;

      await _quarantineTracking.AddAsync(new QuarantinedAttachmentDocument
      {
        FolderPrefix = context.FolderPrefix,
        ProjectId = context.ProjectId,
        UploadedBy = context.UploadedBy,
        DocumentTypeInt = documentTypeInt,
        FolderPartitionId = context.FolderPartitionId,
        QuarantinePath = quarantinePath,
        OriginalFileName = file.FileName,
      }, cancellationToken);

      _backgroundJobService.Enqueue<AttachmentScanJob>(
          job => job.ExecuteAsync(
              context.FolderPrefix,
              context.ProjectId,
              context.UploadedBy,
              documentTypeInt,
              context.FolderPartitionId,
              quarantinePath,
              file.FileName));

      await _auditService.LogQueuedAsync(context, file, quarantinePath, cancellationToken);

      return QuarantineQueueResult.Ok(quarantinePath);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to queue malware scan for {FileName}", file.FileName);

      await _auditService.LogQueueExceptionAsync(context, file, ex, cancellationToken);

      return QuarantineQueueResult.Failed(
          "Không thể đưa tệp đính kèm vào hàng đợi quét mã độc. Vui lòng thử lại sau.",
          abortWorkflow: _scanOptions.FailClosed);
    }
  }
}