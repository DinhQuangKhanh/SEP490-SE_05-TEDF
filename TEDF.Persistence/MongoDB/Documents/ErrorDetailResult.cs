namespace TEDF.Persistence.MongoDB.Documents;

/// <summary>
/// Represents a unique error message with its occurrence count.
/// Used in the admin activity-logs detail view.
/// </summary>
public class ErrorDetailResult
{
    public string Message { get; set; } = string.Empty;
    public string? ErrorType { get; set; }
    public int Count { get; set; }
    public DateTime LatestAt { get; set; }
}
