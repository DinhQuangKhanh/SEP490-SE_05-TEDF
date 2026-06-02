namespace TEDF.Persistence.MongoDB.Documents;

/// <summary>
/// Represents a grouped activity log entry — one row per (UserId, ActiveRole, Action, Category).
/// </summary>
public class GroupedActivityLogResult
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string ActiveRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int TotalCount { get; set; }
    public DateTime LatestTimestamp { get; set; }
    public SeverityCountsResult SeverityCounts { get; set; } = new();
}

public class SeverityCountsResult
{
    public int Info { get; set; }
    public int Warning { get; set; }
    public int Error { get; set; }
    public int Critical { get; set; }
}
