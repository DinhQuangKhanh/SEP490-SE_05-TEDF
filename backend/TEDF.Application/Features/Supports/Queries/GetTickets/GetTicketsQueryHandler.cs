using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Supports.DTOs;

namespace TEDF.Application.Features.Supports.Queries.GetTickets;

public class GetTicketsQueryHandler : IQueryHandler<GetTicketsQuery, List<TicketListDto>>
{
    private readonly ISupportsQueryService _supports;

    public GetTicketsQueryHandler(ISupportsQueryService supports) => _supports = supports;

    public Task<List<TicketListDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
        => _supports.GetTicketsAsync(
            request.ReporterId, request.IsAdmin, request.SearchTerm, request.Status, request.Priority, cancellationToken);
}
