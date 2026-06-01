using UniThesis.API.Common.Security.Abstractions;
using UniThesis.Application.Common.Interfaces;

namespace UniThesis.API.Common.Security.Workflow;

/// <summary>
/// Uploads files to quarantine storage and enqueues background malware scan jobs.
/// Context-agnostic — the <see cref="AttachmentScanContext"/> carries folder paths,
/// document type, and metadata so a single workflow handles all upload scenarios.
/// </summary>
internal sealed class AttachmentScanWorkflow : IAttachmentScanWorkflow
{
  private readonly IAttachmentQuarantineService _quarantineService;
  private readonly ILogger<AttachmentScanWorkflow> _logger;

  public AttachmentScanWorkflow(
      IAttachmentQuarantineService quarantineService,
      ILogger<AttachmentScanWorkflow> logger)
  {
    _quarantineService = quarantineService;
    _logger = logger;
  }

  public async Task<AttachmentQueueResult> QueueAsync(
      AttachmentScanContext context,
      IReadOnlyCollection<IFormFile>? attachments,
      CancellationToken cancellationToken = default)
  {
    if (attachments is null || attachments.Count == 0)
    {
      return AttachmentQueueResult.Ok(0);
    }

    var queuedCount = 0;

    foreach (var file in attachments)
    {
      var queueResult = await _quarantineService.QueueAsync(context, file, cancellationToken);

      if (queueResult.Success)
      {
        queuedCount++;
        continue;
      }

      _logger.LogWarning("Quarantine queue failed for {FileName}: {Error}", file.FileName, queueResult.ErrorMessage);

      if (queueResult.AbortWorkflow)
      {
        return AttachmentQueueResult.Failed(
            "Không thể đưa tệp đính kèm vào hàng đợi quét mã độc. Vui lòng thử lại sau.",
            queuedCount);
      }
    }

    return queuedCount == 0
        ? AttachmentQueueResult.Failed(
            "Không thể đưa tệp đính kèm vào hàng đợi quét mã độc. Vui lòng kiểm tra cấu hình Firebase Storage hoặc xem log audit.",
            queuedCount)
        : AttachmentQueueResult.Ok(queuedCount);
  }
}