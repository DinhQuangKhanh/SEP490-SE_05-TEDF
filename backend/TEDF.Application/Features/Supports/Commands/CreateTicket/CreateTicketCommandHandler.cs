using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Supports.Commands.CreateTicket;

public class CreateTicketCommandHandler : ICommandHandler<CreateTicketCommand, Guid>
{
    private readonly ISupportsDomainService _supports;

    public CreateTicketCommandHandler(ISupportsDomainService supports) => _supports = supports;

    public Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
        => _supports.CreateTicketAsync(
            request.Title, request.Description, request.Category, request.Priority, request.ReporterId, cancellationToken);
}
