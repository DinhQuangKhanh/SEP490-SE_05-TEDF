using System.Text.Json;

namespace TEDF.Application.Features.Projects.DTOs;

public record GetProjectAuditLogsResponse
{
    public IEnumerable<ProjectAuditLogDto> Logs { get; init; } = [];

    /// <summary>Number of times the group was sent back for modification.</summary>
    public int RevisionCount { get; init; }

    /// <summary>Highest submission attempt reached by the project.</summary>
    public int SubmissionCount { get; init; }
}

public record ProjectAuditLogDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public Guid? PerformedBy { get; init; }
    public string? PerformedByName { get; init; }

    /// <summary>Project status before this action, when the action changed the status.</summary>
    public string? OldStatus { get; init; }

    /// <summary>Project status after this action, when the action changed the status.</summary>
    public string? NewStatus { get; init; }

    public int? SubmissionNumber { get; init; }
    public DateTime Timestamp { get; init; }
    public JsonElement? Metadata { get; init; }
}
