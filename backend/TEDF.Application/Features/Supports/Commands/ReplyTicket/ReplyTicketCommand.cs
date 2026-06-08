using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;

using TEDF.Application.Common.Models;

namespace TEDF.Application.Features.Supports.Commands.ReplyTicket;

[ActionLog("Reply Ticket", "Support")]
public record ReplyTicketCommand(
    Guid TicketId,
    Guid SenderId,
    string Content,
    IEnumerable<FileAttachmentDto>? Attachments = null) : ICommand;
