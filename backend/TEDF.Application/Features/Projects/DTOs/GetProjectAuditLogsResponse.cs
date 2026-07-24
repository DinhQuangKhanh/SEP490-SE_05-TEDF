namespace TEDF.Application.Features.Projects.DTOs;

public record GetProjectAuditLogsResponse
{
    public IEnumerable<ProjectAuditLogDto> Logs { get; init; } = [];
    public int RevisionCount { get; init; }
}

public record ProjectAuditLogDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public Guid? PerformedBy { get; init; }
    public string? PerformedByName { get; init; }
    public DateTime Timestamp { get; init; }
    public object? Metadata { get; init; }
}
