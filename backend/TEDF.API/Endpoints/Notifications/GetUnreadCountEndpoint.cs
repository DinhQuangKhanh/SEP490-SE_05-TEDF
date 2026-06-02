using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Notifications.Queries.GetUnreadCount;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Notifications;

/// <summary>
/// Endpoint: GET /api/notifications/unread-count
/// Returns the count of unread notifications for the authenticated user.
/// </summary>
public class GetUnreadCountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications/unread-count", async (
                [Microsoft.AspNetCore.Mvc.FromQuery] TEDF.Domain.Enums.Notification.NotificationCategory? category,
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
