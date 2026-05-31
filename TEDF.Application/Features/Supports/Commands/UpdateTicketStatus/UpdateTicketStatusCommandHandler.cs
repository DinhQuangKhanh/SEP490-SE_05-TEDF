using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Ticket;

namespace TEDF.Application.Features.Supports.Commands.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler : ICommandHandler<UpdateTicketStatusCommand>
{
    private readonly ISupportTicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTicketStatusCommandHandler(ISupportTicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), request.TicketId);

        // Map request to Domain methods to enforce rules and fire events
        switch (request.Status)
        {
            case TicketStatus.InProgress:
                if (ticket.Status == TicketStatus.Open) ticket.StartProgress();
                break;
            case TicketStatus.Resolved:
                ticket.Resolve();
                break;
            case TicketStatus.Closed:
                ticket.Close();
                break;
            case TicketStatus.Open:
                if (ticket.Status == TicketStatus.Closed || ticket.Status == TicketStatus.Resolved)
                    ticket.Reopen();
                break;
        }

        _repository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
