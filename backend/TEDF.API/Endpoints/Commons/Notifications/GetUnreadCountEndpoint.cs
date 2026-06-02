using MediatR;
using TEDF.Application.Features.Notifications.Queries.GetUnreadCount;

namespace TEDF.API.Endpoints.Commons.Notifications;

/// <summary>
/// Endpoint: GET /api/notifications/unread-count
/// Returns the count of unread notifications for the authenticated user.
/// </summary>
public class GetUnreadCountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications/unread-count", async (
                [Microsoft.AspNetCore.Mvc.FromQuery] Domain.Enums.Notification.NotificationCategory? category,
                ISender sender,
                CancellationToken cancellationToken = default) =>
            {
                var count = await sender.Send(new GetUnreadCountQuery(), cancellationToken);
                return Results.Ok(new { UnreadCount = count });
            })
            .RequireAuthorization()
            .WithTags("Notifications")
            .WithName("GetUnreadCount")
            .Produces(200)
            .Produces(401);
    }
}
