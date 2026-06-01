using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.SupportAggregate;
using TEDF.Domain.Aggregates.SupportAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Application.Features.Supports.Commands.CreateTicket;

public class CreateTicketCommandHandler : ICommandHandler<CreateTicketCommand, Guid>
{
    private readonly ISupportTicketRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTicketCommandHandler(ISupportTicketRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        var year = DateTime.UtcNow.Year;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var seq = await _repository.GetNextSequenceAsync(year, cancellationToken);
            var code = TicketCode.Generate(year, seq);

            var ticket = SupportTicket.Create(
                code,
                request.Title,
                request.Description,
                request.ReporterId,
                request.Category,
                request.Priority);

            try
            {
                await _repository.AddAsync(ticket, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ticket.Id;
            }
            catch (DbUpdateException ex) when (IsDuplicateCodeViolation(ex))
            {
                _repository.Remove(ticket);

                if (attempt == maxAttempts)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("Failed to create support ticket after retry attempts.");
    }

    private static bool IsDuplicateCodeViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not SqlException sqlException)
        {
            return false;
        }

        return sqlException.Number is 2601 or 2627;
    }
}
