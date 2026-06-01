using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;

namespace TEDF.Application.Features.Supports.Queries.GetTicketStats;

public record GetTicketStatsQuery(Guid ReporterId, bool IsAdmin) : IQuery<TicketStatsDto>;
