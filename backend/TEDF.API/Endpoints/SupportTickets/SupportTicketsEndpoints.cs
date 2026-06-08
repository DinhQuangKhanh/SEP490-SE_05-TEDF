using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.SupportTickets.Requests;
using TEDF.API.Extensions;
using TEDF.Application.Features.Supports.Commands.CreateTicket;
using TEDF.Application.Features.Supports.Commands.ReplyTicket;
using TEDF.Application.Features.Supports.Commands.UpdateTicketStatus;
using TEDF.Application.Features.Supports.Queries.GetTicketById;
using TEDF.Application.Features.Supports.Queries.GetTickets;
using TEDF.Application.Features.Supports.Queries.GetTicketStats;
using TEDF.Domain.Enums.Ticket;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.SupportTickets;

public sealed class SupportTicketsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/support-tickets").RequireAuthorization();

        group.MapGet("", GetTickets).WithTags("SupportTickets").WithName("GetTickets").Produces(200).Produces(401);
        group.MapGet("/stats", GetTicketStats).WithTags("SupportTickets").WithName("GetTicketStats").Produces(200).Produces(401);
        group.MapGet("/{id:guid}", GetTicketById).WithTags("SupportTickets").WithName("GetTicketById").Produces(200).Produces(401);
        group.MapPost("", CreateTicket).WithTags("SupportTickets").WithName("CreateTicket").Produces(201).Produces(400).Produces(401);
        group.MapPost("/{id:guid}/reply", ReplyTicket).WithTags("SupportTickets").WithName("ReplyTicket").Produces(200).Produces(400).Produces(401);
        group.MapPatch("/{id:guid}/status", UpdateTicketStatus).WithTags("SupportTickets").WithName("UpdateTicketStatus").Produces(200).Produces(400).Produces(401);
    }

    private static async Task<IResult> GetTickets([AsParameters] GetTicketsRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var isAdmin = context.User.IsInRole("Admin");
        var result = await sender.Send(new GetTicketsQuery(reporterId, isAdmin, request.SearchTerm, request.Status, request.Priority), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTicketStats(ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var isAdmin = context.User.IsInRole("Admin");
        var result = await sender.Send(new GetTicketStatsQuery(reporterId, isAdmin), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTicketById(Guid id, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetTicketByIdQuery(id), ct));

    private static async Task<IResult> CreateTicket(CreateTicketRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var reporterId = context.User.GetUserId();
        var command = new CreateTicketCommand(request.Title, request.Description, request.Category, request.Priority, reporterId);
        var ticketId = await sender.Send(command, ct);
        return Created($"/api/support-tickets/{ticketId}", new { Id = ticketId }, "Tạo mới thành công.");
    }

    private static async Task<IResult> ReplyTicket(Guid id, [FromBody] ReplyTicketRequest request, ISender sender, HttpContext context, CancellationToken ct)
    {
        var senderId = context.User.GetUserId();
        await sender.Send(new ReplyTicketCommand(id, senderId, request.Content), ct);
        if (context.User.IsInRole("Admin"))
            await sender.Send(new UpdateTicketStatusCommand(id, TicketStatus.InProgress), ct);
        return Ok("Phản hồi thành công.");
    }

    private static async Task<IResult> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UpdateTicketStatusCommand(id, request.Status), ct);
        return Ok("Cập nhật thành công.");
    }
}
