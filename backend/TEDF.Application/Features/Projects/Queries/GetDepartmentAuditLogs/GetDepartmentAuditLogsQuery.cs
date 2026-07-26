using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentAuditLogs;

/// <param name="Search">Matches project code, project name or the performer's name.</param>
/// <param name="Actions">Comma-separated <c>ProjectAuditAction</c> names; empty means all.</param>
/// <param name="SemesterId">Restricts the trail to one semester; null spans every semester.</param>
/// <param name="From">Inclusive lower bound on the action timestamp (UTC).</param>
/// <param name="To">Inclusive upper bound on the action timestamp (UTC).</param>
public record GetDepartmentAuditLogsQuery(
    string? Search,
    string? Actions,
    int? SemesterId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 10) : IQuery<GetDepartmentAuditLogsResponse>;
