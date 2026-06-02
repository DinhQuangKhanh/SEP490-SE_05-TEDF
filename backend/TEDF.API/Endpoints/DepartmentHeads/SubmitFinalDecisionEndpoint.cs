using MediatR;
using TEDF.API.Endpoints.DepartmentHeads.Requests;
using TEDF.Application.Features.Departments.Commands.SubmitFinalDecision;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHeads;

public class SubmitFinalDecisionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/department-head/projects/{projectId:guid}/final-decision", async (
                Guid projectId,
                SubmitFinalDecisionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new SubmitFinalDecisionCommand(projectId, request.Result, request.Notes);
                await sender.Send(command, cancellationToken);
                return NoContent("Quyết định cuối cùng đã được gửi thành công.");
            })
             .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("SubmitFinalDecision")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
