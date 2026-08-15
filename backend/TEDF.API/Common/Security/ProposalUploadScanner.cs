using TEDF.API.Common.Security.Abstractions;
using TEDF.Domain.Enums.Document;

namespace TEDF.API.Common.Security;

/// <summary>
/// Queues a topic proposal's uploads (the capstone register form + any free attachments) through the
/// malware-scan workflow, so they are quarantined, scanned in the background, and promoted to viewable
/// topic documents (<see cref="DocumentType.Proposal"/> for the register form so the UI can label it
/// "Phiếu đăng ký").
///
/// Queueing is <b>best-effort</b>: the topic has already been created by the time this runs, so a queue
/// failure never throws — it is surfaced in the returned <see cref="AttachmentQueueResult"/> for the
/// caller to log. Extracted from the endpoint so it can be unit-tested with a mocked
/// <see cref="IAttachmentScanWorkflow"/> instead of hosting the whole minimal-API pipeline.
/// </summary>
internal static class ProposalUploadScanner
{
    public static async Task<AttachmentQueueResult> QueueAsync(
        IAttachmentScanWorkflow scanWorkflow,
        Guid projectId,
        Guid uploaderId,
        IFormFile? registerForm,
        IReadOnlyCollection<IFormFile>? attachments,
        CancellationToken cancellationToken = default)
    {
        var queuedCount = 0;
        string? firstError = null;

        if (registerForm is { Length: > 0 })
        {
            var context = new AttachmentScanContext(
                FolderPrefix: "registration-forms",
                ProjectId: projectId,
                UploadedBy: uploaderId,
                FolderPartitionId: projectId,
                DocumentType: DocumentType.Proposal);

            var result = await scanWorkflow.QueueAsync(context, new[] { registerForm }, cancellationToken);
            queuedCount += result.QueuedCount;
            if (!result.Success)
                firstError ??= result.ErrorMessage;
        }

        var files = attachments?.Where(f => f.Length > 0).ToList();
        if (files is { Count: > 0 })
        {
            var context = new AttachmentScanContext(
                FolderPrefix: "topic-proposal-attachments",
                ProjectId: projectId,
                UploadedBy: uploaderId,
                FolderPartitionId: projectId,
                DocumentType: DocumentType.Reference);

            var result = await scanWorkflow.QueueAsync(context, files, cancellationToken);
            queuedCount += result.QueuedCount;
            if (!result.Success)
                firstError ??= result.ErrorMessage;
        }

        return firstError is null
            ? AttachmentQueueResult.Ok(queuedCount)
            : AttachmentQueueResult.Failed(firstError, queuedCount);
    }
}
