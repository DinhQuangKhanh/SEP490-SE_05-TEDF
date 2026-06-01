using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Notifications.Commands.MarkAsRead;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Notifications;

/// <summary>
/// Endpoint: PUT /api/notifications/{id}/read
/// Marks a specific notification as read for the authenticated user.
/// </summary>
public class MarkNotificationAsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/notifications/{id:guid}/read", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                await sender.Send(
                    new MarkNotificationAsReadCommand(id), cancellationToken);
                return NoContent("Thành công.");
            })
            .RequireAuthorization()
            .WithTags("Notifications")
            .WithName("MarkNotificationAsRead")
            .Produces(204)
            .Produces(401);
    }
}
