using MediatR;
using TEDF.Application.Features.Users.Commands.UnlockUser;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins;

public class UnlockUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/users/{userId:guid}/unlock", async (
                Guid userId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new UnlockUserCommand(userId), cancellationToken);
                return NoContent("Mở khóa thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Admin")
            .WithName("UnlockUser")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
