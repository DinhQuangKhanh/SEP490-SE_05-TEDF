using MongoDB.Bson.Serialization.Attributes;

namespace TEDF.Persistence.MongoDB.Documents;

/// <summary>
/// Tracks files currently in quarantine storage awaiting malware scanning.
/// Written when a file is enqueued; deleted when scan completes (promoted or permanently failed).
/// Used by QuarantineRetryJob to find and re-enqueue files stuck in quarantine.
/// </summary>
public class QuarantinedAttachmentDocument
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FolderPrefix { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid UploadedBy { get; set; }
    public int DocumentTypeInt { get; set; }
    public Guid FolderPartitionId { get; set; }
    public string QuarantinePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Set after the quarantine file has been promoted to clean storage.
    /// If non-null, the quarantine file is gone but the Document entity may not yet exist —
    /// the scan job should create the Document using this path instead of downloading from quarantine.
    /// </summary>
    public string? CleanPath { get; set; }
}
