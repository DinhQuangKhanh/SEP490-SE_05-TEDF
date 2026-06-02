using MediatR;
using TEDF.API.Extensions;
using TEDF.Application.Features.Notifications.DTOs;
using TEDF.Application.Features.Notifications.Queries.GetUserNotifications;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Notifications;

/// <summary>
/// Endpoint: GET /api/notifications
/// Returns paginated notifications for the authenticated user.
/// </summary>
public class GetNotificationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notifications", async (
                ISender sender,
                int limit = 50,
                CancellationToken cancellationToken = default) =>
            {
                if (limit is < 1 or > 200) limit = 50;

                var result = await sender.Send(
                    new GetUserNotificationsQuery(limit), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization()
            .WithTags("Notifications")
            .WithName("GetNotifications")
            .Produces<NotificationListResponseDto>()
            .Produces(401);
    }
}
