using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentAuditLogs;

/// <param name="Search">Matches project code, project name or the performer's name.</param>
/// <param name="Actions">Comma-separated <c>ProjectAuditAction</c> names; empty means all.</param>
public record GetDepartmentAuditLogsQuery(
    string? Search,
    string? Actions,
    int Page = 1,
    int PageSize = 10) : IQuery<GetDepartmentAuditLogsResponse>;
