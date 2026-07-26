namespace TEDF.Application.Features.Projects.DTOs;

/// <summary>
/// Paged audit trail across every project in the department head's department.
/// </summary>
public record GetDepartmentAuditLogsResponse
{
    public IEnumerable<DepartmentAuditLogItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    /// <summary>Totals over the whole department scope, not just the current page.</summary>
    public DepartmentAuditLogStatsDto Stats { get; init; } = new();
}

public record DepartmentAuditLogItemDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Guid? PerformedBy { get; init; }
    public string? PerformedByName { get; init; }
    public string? OldStatus { get; init; }
    public string? NewStatus { get; init; }
    public int? SubmissionNumber { get; init; }
    public DateTime Timestamp { get; init; }
    public object? Metadata { get; init; }
}

public record DepartmentAuditLogStatsDto
{
    public int Total { get; init; }
    public int Submitted { get; init; }
    public int Approved { get; init; }
    public int Revision { get; init; }
    public int Rejected { get; init; }
}
