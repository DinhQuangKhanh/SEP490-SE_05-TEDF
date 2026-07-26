namespace TEDF.API.Endpoints.Projects.Requests;

/// <param name="Search">Matches project code, project name or the performer's name.</param>
/// <param name="Actions">Comma-separated <c>ProjectAuditAction</c> names; empty means all.</param>
/// <param name="SemesterId">Restricts the trail to one semester; null spans every semester.</param>
/// <param name="From">Inclusive lower bound on the action timestamp (UTC).</param>
/// <param name="To">Inclusive upper bound on the action timestamp (UTC).</param>
public record GetDepartmentAuditLogsRequest(
    string? Search,
    string? Actions,
    int? SemesterId,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 10);
