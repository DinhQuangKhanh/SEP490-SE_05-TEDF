using MediatR;
using TEDF.API.Endpoints.Admins.Requests;
using TEDF.Application.Features.Departments.Commands.AssignDepartmentHead;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins;

public class AssignDepartmentHeadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/departments/{departmentId:int}/head", async (
                int departmentId,
                AssignDepartmentHeadRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new AssignDepartmentHeadCommand(departmentId, request.UserId);
                await sender.Send(command, cancellationToken);
                return NoContent("Thiết lập chở bộ phậm thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Admin")
            .WithName("AssignDepartmentHead")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
