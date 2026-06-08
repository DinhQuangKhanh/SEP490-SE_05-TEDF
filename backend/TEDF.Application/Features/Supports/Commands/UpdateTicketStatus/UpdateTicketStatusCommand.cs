using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Attributes;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Application.Features.Supports.Commands.UpdateTicketStatus;

[ActionLog("Update Ticket Status", "Support")]
public record UpdateTicketStatusCommand(
    Guid TicketId,
    TicketStatus Status) : ICacheInvalidatingCommand
{
    public IReadOnlyCollection<string> CachePrefixesToInvalidate =>
        ["tickets:"];
}
