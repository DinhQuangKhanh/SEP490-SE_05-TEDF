using MediatR;
using TEDF.Application.Features.Notifications.Commands.MarkAllAsRead;
using TEDF.Application.Features.Notifications.Commands.MarkAsRead;
using TEDF.Application.Features.Notifications.Queries.GetUnreadCount;
using TEDF.Application.Features.Notifications.Queries.GetUserNotifications;
using TEDF.Application.Features.Notifications.DTOs;
using static TEDF.API.Extensions.ApiResponseExtensions;
using Microsoft.AspNetCore.Mvc;
using TEDF.Domain.Enums.Notification;

namespace TEDF.API.Endpoints.Notifications;

public sealed class NotificationsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").RequireAuthorization();

        group.MapGet("", GetNotifications)
            .WithTags("Notifications")
            .WithName("GetNotifications")
            .Produces<NotificationListResponseDto>()
            .Produces(401);

        group.MapGet("/unread-count", GetUnreadCount)
            .WithTags("Notifications")
            .WithName("GetUnreadCount")
            .Produces(200).Produces(401);

        group.MapPut("/read-all", MarkAllAsRead)
            .WithTags("Notifications")
            .WithName("MarkAllAsRead")
            .Produces(204).Produces(401);

        group.MapPut("/{id:guid}/read", MarkAsRead)
            .WithTags("Notifications")
            .WithName("MarkNotificationAsRead")
            .Produces(204).Produces(401);
    }

    private static async Task<IResult> GetNotifications(
        ISender sender, int limit = 50, CancellationToken ct = default)
    {
        if (limit is < 1 or > 200) limit = 50;
        var result = await sender.Send(new GetUserNotificationsQuery(limit), ct);
        return Ok(result);
    }

    private static async Task<IResult> GetUnreadCount(
        [FromQuery] NotificationCategory? category,
        ISender sender,
        CancellationToken ct = default)
    {
        var count = await sender.Send(new GetUnreadCountQuery(), ct);
        return Results.Ok(new { UnreadCount = count });
    }

    private static async Task<IResult> MarkAllAsRead(ISender sender, CancellationToken ct = default)
    {
        await sender.Send(new MarkAllNotificationsAsReadCommand(), ct);
        return NoContent("Thành công.");
    }

    private static async Task<IResult> MarkAsRead(Guid id, ISender sender, CancellationToken ct = default)
    {
        await sender.Send(new MarkNotificationAsReadCommand(id), ct);
        return NoContent("Thành công.");
    }
}
