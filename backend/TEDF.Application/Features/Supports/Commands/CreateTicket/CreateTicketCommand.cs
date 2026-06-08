using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Domain.Enums.Ticket;

using TEDF.Application.Common.Models;

namespace TEDF.Application.Features.Supports.Commands.CreateTicket;

[ActionLog("Create Ticket", "Support")]
public record CreateTicketCommand(
    string Title,
    string Description,
    TicketCategory Category,
    TicketPriority Priority,
    Guid ReporterId,
    IEnumerable<FileAttachmentDto>? Attachments = null) : ICommand<Guid>;
