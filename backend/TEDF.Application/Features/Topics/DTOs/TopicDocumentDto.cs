namespace TEDF.Application.Features.Topics.DTOs;

/// <summary>
/// DTO for a document attached to a thesis topic/project.
/// </summary>
public class TopicDocumentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;

    /// <summary>Public storage URL, so the client can preview or download the file.</summary>
    public string FileUrl { get; init; } = string.Empty;

    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime UploadedAt { get; init; }
    public string UploadedByName { get; init; } = string.Empty;
}
