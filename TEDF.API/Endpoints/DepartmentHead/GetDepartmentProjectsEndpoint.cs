using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Departments.Queries.GetDepartmentProjects;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHead;

public class GetDepartmentProjectsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/department-head/projects", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetDepartmentProjectsQuery(), cancellationToken);
                return Ok(result);
            })
             .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentProjects");
    }
}
