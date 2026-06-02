using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.Supports.Requests;
using TEDF.API.Extensions;
using TEDF.Application.Features.Supports.Commands.CreateTicket;
using TEDF.Application.Features.Supports.Commands.ReplyTicket;
using TEDF.Application.Features.Supports.Commands.UpdateTicketStatus;
using TEDF.Domain.Enums.Ticket;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Supports;

public partial class SupportEndpoints : IEndpoint
{
    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Commands: các endpoint làm thay đổi dữ liệu/state
        // ─────────────────────────────────────────────────────────────

        #region Tạo ticket mới

        // POST /api/supports
        // Người dùng hiện tại tạo một ticket hỗ trợ mới.
        group.MapPost("", CreateTicket)
            .WithName("CreateTicket")
            .WithTags("Supports");

        #endregion

        #region Phản hồi ticket

        // POST /api/supports/{id}/reply
        // Gửi một phản hồi vào ticket. Nếu Admin phản hồi thì ticket được tự chuyển sang InProgress.
        group.MapPost("{id:guid}/reply", ReplyTicket)
            .WithName("ReplyTicket")
            .WithTags("Supports");

        #endregion

        #region Cập nhật trạng thái ticket

        // PATCH /api/supports/{id}/status
        // Cập nhật trạng thái của một ticket.
        group.MapPatch("{id:guid}/status", UpdateTicketStatus)
            .WithName("UpdateTicketStatus")
            .WithTags("Supports");

        #endregion
    }

    #region Handler: tạo ticket mới

    private static async Task<IResult> CreateTicket(CreateTicketRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var command = new CreateTicketCommand(request.Title, request.Description, request.Category, request.Priority,
            reporterId);

        var ticketId = await sender.Send(command, ct);
        return Created($"/api/supports/{ticketId}", new { Id = ticketId }, "Tạo mới thành công.");
    }

    #endregion

    #region Handler: phản hồi ticket

    private static async Task<IResult> ReplyTicket(Guid ticketId, [FromBody] ReplyTicketRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var senderId = context.User.GetUserId();
        var command = new ReplyTicketCommand(ticketId, senderId, request.Content);
        await sender.Send(command, ct);

        // Auto update to InProgress if an admin replies and it's open.
        // We'll optimistically send an update command. Domain handles ignoring if not open.
        if (context.User.IsInRole("Admin"))
        {
            await sender.Send(new UpdateTicketStatusCommand(ticketId, TicketStatus.InProgress), ct);
        }

        return Ok("Phản hồi thành công.");
    }

    #endregion

    #region Handler: cập nhật trạng thái ticket

    private static async Task<IResult> UpdateTicketStatus(Guid ticketId, [FromBody] UpdateTicketStatusRequest request, ISender sender, CancellationToken ct)
    {
        var command = new UpdateTicketStatusCommand(ticketId, request.Status);
        await sender.Send(command, ct);
        return Ok("Cập nhật thành công.");
    }

    #endregion
}
