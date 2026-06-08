using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Supports.DTOs;

namespace TEDF.Application.Features.Supports.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IQueryHandler<GetTicketByIdQuery, TicketDto>
{
    private readonly ISupportsQueryService _supports;

    public GetTicketByIdQueryHandler(ISupportsQueryService supports) => _supports = supports;

    public Task<TicketDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
        => _supports.GetByIdAsync(request.Id, cancellationToken);
}
