using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Departments.Queries.GetDepartmentEvaluators;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHead;

public class GetDepartmentEvaluatorsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/department-head/evaluators", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetDepartmentEvaluatorsQuery(), cancellationToken);
                return Ok(result);
            })
             .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("GetDepartmentEvaluators");
    }
}
