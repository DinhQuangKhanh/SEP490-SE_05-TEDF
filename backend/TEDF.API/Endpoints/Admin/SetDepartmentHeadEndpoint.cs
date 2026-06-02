using MediatR;
using TEDF.API.Extensions;
using TEDF.API.Endpoints.Admin.Requests;
using TEDF.Application.Features.Departments.Commands.SetDepartmentHead;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class SetDepartmentHeadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/departments/{departmentId:int}/head", async (
                int departmentId,
                SetDepartmentHeadRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var command = new SetDepartmentHeadCommand(departmentId, request.UserId);
                await sender.Send(command, cancellationToken);
                return NoContent("Thiết lập chở bộ phậm thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Admin")
            .WithName("SetDepartmentHead")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
