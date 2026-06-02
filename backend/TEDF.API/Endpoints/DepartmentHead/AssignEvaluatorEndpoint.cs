using MediatR;
using TEDF.API.Extensions;
using TEDF.API.Endpoints.DepartmentHead.Requests;
using TEDF.Application.Features.Departments.Commands.AssignEvaluator;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHead;

public class AssignEvaluatorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/department-head/assign-evaluator", async (
                AssignEvaluatorRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AssignEvaluatorCommand(
                    request.ProjectId,
                    request.EvaluatorId,
                    request.EvaluatorOrder);

                await sender.Send(command, cancellationToken);
                return NoContent("Gán người thẩm định thành công.");
            })
             .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("AssignEvaluator")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);
    }
}
