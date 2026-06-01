using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Supports.DTOs;

namespace TEDF.Application.Features.Supports.Queries.GetTicketById;

public record GetTicketByIdQuery(Guid Id) : IQuery<TicketDto>;
