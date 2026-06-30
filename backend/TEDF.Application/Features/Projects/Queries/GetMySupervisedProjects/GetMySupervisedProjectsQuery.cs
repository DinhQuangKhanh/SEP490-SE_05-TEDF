using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Projects.Queries.GetMySupervisedProjects;

/// <summary>
/// Query for the projects the authenticated mentor actively supervises.
/// Supports quick search (by topic name/code), sorting and pagination.
/// Sort: "name" | "oldest" | "status" | default newest.
/// </summary>
public record GetMySupervisedProjectsQuery(
    string? Search = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = 10
) : IQuery<GetMySupervisedProjectsResult>;
