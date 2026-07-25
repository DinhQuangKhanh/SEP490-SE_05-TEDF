namespace TEDF.API.Endpoints.ActivityLogs.Requests;

public record GetActivityLogsRequest(
    string? Role,
    string? FeatureCategory,
    string? Status,
    string? Search,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);

public record GetErrorLogsRequest(
    string? Severity,
    string? Source,
    string? Search,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 20);
