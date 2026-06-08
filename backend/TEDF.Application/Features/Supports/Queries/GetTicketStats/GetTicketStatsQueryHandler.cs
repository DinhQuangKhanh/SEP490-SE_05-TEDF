using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Supports.DTOs;

namespace TEDF.Application.Features.Supports.Queries.GetTicketStats;

public class GetTicketStatsQueryHandler : IQueryHandler<GetTicketStatsQuery, TicketStatsDto>
{
    private readonly ISupportsQueryService _supports;

    public GetTicketStatsQueryHandler(ISupportsQueryService supports) => _supports = supports;

    public Task<TicketStatsDto> Handle(GetTicketStatsQuery request, CancellationToken cancellationToken)
        => _supports.GetStatsAsync(request.ReporterId, request.IsAdmin, cancellationToken);
}
