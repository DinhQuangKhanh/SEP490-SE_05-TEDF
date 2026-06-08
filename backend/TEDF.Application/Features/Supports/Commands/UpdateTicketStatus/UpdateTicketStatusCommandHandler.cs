using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Supports.Commands.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler : ICommandHandler<UpdateTicketStatusCommand>
{
    private readonly ISupportsDomainService _supports;

    public UpdateTicketStatusCommandHandler(ISupportsDomainService supports) => _supports = supports;

    public async Task<Unit> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        await _supports.UpdateStatusAsync(request.TicketId, request.Status, cancellationToken);
        return Unit.Value;
    }
}
