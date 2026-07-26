using MediatR;
using TEDF.Application.Features.Projects.Queries.GetDepartmentAuditLogs;
using TEDF.Application.Features.Projects.Queries.GetDepartmentProjects;
using TEDF.Application.Features.Projects.Queries.GetMySupervisedProjects;
using TEDF.Application.Features.Projects.Queries.GetProjects;
using TEDF.Application.Features.Projects.Queries.GetProjectAuditLogs;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Projects;

public sealed class ProjectsEndpoints : IEndpoint
{
    private const string Tag = "Projects";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        // Admin oversight of all projects.
        group.MapGet("", GetProjects)
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags(Tag).WithName("GetProjects")
            .Produces(200).Produces(401);

        // Department head: projects within the caller's department.
        group.MapGet("/department", GetDepartmentProjects)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags(Tag).WithName("GetDepartmentProjects")
            .Produces(200).Produces(401).Produces(403);

        // Mentor: projects the current user supervises (for the profile supervision history).
        group.MapGet("/supervised", GetMySupervisedProjects)
            .WithTags(Tag).WithName("GetMySupervisedProjects")
            .Produces(200).Produces(401);

        // Department head: approval audit trail across the whole department.
        group.MapGet("/audit-logs", GetDepartmentAuditLogs)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags(Tag).WithName("GetDepartmentAuditLogs")
            .Produces(200).Produces(401).Produces(403);

        // Audit logs for a project.
        group.MapGet("/{id:guid}/audit-logs", GetProjectAuditLogs)
            .WithTags(Tag).WithName("GetProjectAuditLogs")
            .Produces(200).Produces(401);
    }

    private static async Task<IResult> GetProjects(
        ISender sender,
        string? search,
        int? semesterId,
        string? status,
        int? majorId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetProjectsQuery(search, semesterId, status, majorId, page, pageSize), ct));

    private static async Task<IResult> GetDepartmentProjects(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDepartmentProjectsQuery(), ct));

    private static async Task<IResult> GetMySupervisedProjects(
        ISender sender, string? search, string? sort, int page = 1, int pageSize = 10, CancellationToken ct = default)
        => Ok(await sender.Send(new GetMySupervisedProjectsQuery(search, sort, page, pageSize), ct));

    private static async Task<IResult> GetProjectAuditLogs(ISender sender, Guid id, CancellationToken ct = default)
        => Ok(await sender.Send(new GetProjectAuditLogsQuery(id), ct));

    private static async Task<IResult> GetDepartmentAuditLogs(
        ISender sender, string? search, string? actions,
        int page = 1, int pageSize = 10, CancellationToken ct = default)
        => Ok(await sender.Send(new GetDepartmentAuditLogsQuery(search, actions, page, pageSize), ct));
}
