using MediatR;
using TEDF.Application.Features.Notifications.Commands.MarkAllAsRead;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Commons.Notifications;

/// <summary>
/// Endpoint: PUT /api/notifications/read-all
/// Marks all notifications as read for the authenticated user.
/// </summary>
public class MarkAllAsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/notifications/read-all", async (
                ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                await sender.Send(
                    new MarkAllNotificationsAsReadCommand(), cancellationToken);
                return NoContent("Thành công.");
            })
            .RequireAuthorization()
            .WithTags("Notifications")
            .WithName("MarkAllAsRead")
            .Produces(204)
            .Produces(401);
    }
}
