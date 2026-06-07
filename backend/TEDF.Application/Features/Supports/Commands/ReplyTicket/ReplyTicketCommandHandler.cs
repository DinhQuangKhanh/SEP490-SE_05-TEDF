using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Supports.Commands.ReplyTicket;

public class ReplyTicketCommandHandler : ICommandHandler<ReplyTicketCommand>
{
    private readonly ISupportsDomainService _supports;

    public ReplyTicketCommandHandler(ISupportsDomainService supports) => _supports = supports;

    public async Task<Unit> Handle(ReplyTicketCommand request, CancellationToken cancellationToken)
    {
        await _supports.ReplyAsync(request.TicketId, request.SenderId, request.Content, cancellationToken);
        return Unit.Value;
    }
}
