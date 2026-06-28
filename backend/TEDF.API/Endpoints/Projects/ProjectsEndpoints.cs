using MediatR;
using TEDF.Application.Features.Projects.Queries.GetDepartmentProjects;
using TEDF.Application.Features.Projects.Queries.GetMySupervisedProjects;
using TEDF.Application.Features.Projects.Queries.GetProjects;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Projects;

public sealed class ProjectsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        // Admin oversight of all projects.
        group.MapGet("", GetProjects)
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Projects").WithName("GetProjects")
            .Produces(200).Produces(401);

        // Department head: projects within the caller's department.
        group.MapGet("/department", GetDepartmentProjects)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("Projects").WithName("GetDepartmentProjects")
            .Produces(200).Produces(401).Produces(403);

        // Mentor: projects the current user supervises (for the profile supervision history).
        group.MapGet("/supervised", GetMySupervisedProjects)
            .WithTags("Projects").WithName("GetMySupervisedProjects")
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

    private static async Task<IResult> GetMySupervisedProjects(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMySupervisedProjectsQuery(), ct));
}
