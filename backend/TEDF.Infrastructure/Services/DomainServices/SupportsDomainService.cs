using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Ticket;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Supports feature. See <see cref="ISupportsDomainService"/>.
/// </summary>
public class SupportsDomainService : ISupportsDomainService
{
    private readonly ISupportTicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SupportsDomainService(ISupportTicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateTicketAsync(
        string title, string description, TicketCategory category, TicketPriority priority,
        Guid reporterId, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;
        var year = DateTime.UtcNow.Year;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var seq = await _repository.GetNextSequenceAsync(year, cancellationToken);
            var code = TicketCode.Generate(year, seq);

            var ticket = SupportTicket.Create(code, title, description, reporterId, category, priority);

            try
            {
                await _repository.AddAsync(ticket, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ticket.Id;
            }
            catch (DbUpdateException ex) when (IsDuplicateCodeViolation(ex))
            {
                _repository.Remove(ticket);
                if (attempt == maxAttempts) throw;
            }
        }

        throw new InvalidOperationException("Failed to create support ticket after retry attempts.");
    }

    public async Task ReplyAsync(Guid ticketId, Guid senderId, string content, CancellationToken cancellationToken = default)
    {
        var ticket = await _repository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), ticketId);

        ticket.AddMessage(senderId, content);

        // Explicitly mark as modified so EF emits the UPDATE (UpdatedAt set inside AddMessage).
        _repository.Update(ticket);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await _repository.GetByIdAsync(ticketId, cancellationToken);
            if (exists is null)
                throw new EntityNotFoundException(nameof(SupportTicket), ticketId);
            throw;
        }
    }

    public async Task UpdateStatusAsync(Guid ticketId, TicketStatus status, CancellationToken cancellationToken = default)
    {
        var ticket = await _repository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(SupportTicket), ticketId);

        switch (status)
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
    }

    private static bool IsDuplicateCodeViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
            return false;
        return sqlException.Number is 2601 or 2627;
    }
}
