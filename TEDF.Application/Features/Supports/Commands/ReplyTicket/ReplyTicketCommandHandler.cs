using MediatR;
using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Application.Features.Supports.Commands.ReplyTicket;

public class ReplyTicketCommandHandler : ICommandHandler<ReplyTicketCommand>
{
    private readonly ISupportTicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ReplyTicketCommandHandler(ISupportTicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ReplyTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetByIdAsync(request.TicketId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), request.TicketId);

        ticket.AddMessage(request.SenderId, request.Content);

        // Explicitly mark as modified to ensure EF generates the UPDATE statement.
        // Without this, EF may fail to detect changes to UpdatedAt (set inside AddMessage)
        // when the entity was loaded in the same DbContext scope, causing
        // DbUpdateConcurrencyException ("expected 1 row, affected 0").
        _repository.Update(ticket);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Row was deleted between load and save, or the entity doesn't exist in DB
            // (e.g. seeder failed). Re-check existence to give a clear error.
            var exists = await _repository.GetByIdAsync(request.TicketId, cancellationToken);
            if (exists is null)
                throw new EntityNotFoundException(nameof(SupportTicket), request.TicketId);

            throw; // genuine concurrency conflict — bubble up
        }

        return Unit.Value;
    }
}
