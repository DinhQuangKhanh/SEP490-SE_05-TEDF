using TEDF.API.Common.Security.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Document;

namespace TEDF.API.Common.Security;

internal enum SyncUploadStatus
{
    Stored,
    Infected,
    ScannerUnavailable,
    StorageFailed,
}

internal sealed record SyncUploadResult(SyncUploadStatus Status, string? Message = null);

/// <summary>
/// Scans a single upload for malware and — only if clean — stores it as a viewable project document,
/// all within the request. Unlike the async quarantine → Hangfire → scan → promote pipeline, this
/// makes "upload → scan → visible for preview" happen in ~1-2s: one storage write (no quarantine/move),
/// no background-job latency, and the document row is committed before the endpoint returns, so the
/// client's immediate refetch already sees it.
///
/// Reuses <see cref="IMalwareScanner"/> (which honours the Enabled / FailClosed options), so a missing
/// or unreachable scanner is surfaced right away instead of leaving the file in silent limbo.
/// Extracted from the endpoint so it can be unit-tested with fakes for the three services.
/// </summary>
internal static class SynchronousAttachmentStore
{
    public static async Task<SyncUploadResult> ScanStoreAsync(
        IMalwareScanner scanner,
        IFileStorageService storage,
        IProjectDocumentWriteService documentWriter,
        string folderPrefix,
        Guid projectId,
        Guid uploaderId,
        DocumentType documentType,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // 1) Scan the bytes BEFORE they ever reach clean storage.
        var scan = await scanner.ScanAsync([file], cancellationToken);
        if (scan.ScannerUnavailable)
            return new SyncUploadResult(SyncUploadStatus.ScannerUnavailable, scan.Message);
        if (!scan.IsClean)
            return new SyncUploadResult(SyncUploadStatus.Infected, scan.Message);

        // 2) Store the clean file directly (single write — no quarantine + move round trips).
        await using var stream = file.OpenReadStream();
        var upload = await storage.UploadAsync(stream, file.FileName, $"{folderPrefix}/{projectId:N}", cancellationToken);
        if (!upload.Success || string.IsNullOrWhiteSpace(upload.FilePath))
            return new SyncUploadResult(SyncUploadStatus.StorageFailed, upload.Error ?? "Không thể lưu tệp lên kho lưu trữ.");

        // 3) Persist the document row (InsertDocumentAsync retires the previous register form for Proposal).
        await documentWriter.InsertDocumentAsync(
            projectId,
            fileName: Path.GetFileName(upload.FilePath),
            originalFileName: file.FileName,
            fileType: Path.GetExtension(file.FileName).ToLowerInvariant(),
            fileSize: file.Length,
            filePath: upload.FilePath,
            documentType: documentType,
            uploadedBy: uploaderId,
            cancellationToken: cancellationToken);

        return new SyncUploadResult(SyncUploadStatus.Stored);
    }
}
