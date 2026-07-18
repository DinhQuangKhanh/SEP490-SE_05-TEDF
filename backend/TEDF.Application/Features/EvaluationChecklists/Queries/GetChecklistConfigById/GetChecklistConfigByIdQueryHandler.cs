using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigById;

public class GetChecklistConfigByIdQueryHandler : IQueryHandler<GetChecklistConfigByIdQuery, ChecklistConfigDto?>
{
    private readonly IChecklistQueryService _queryService;

    public GetChecklistConfigByIdQueryHandler(IChecklistQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<ChecklistConfigDto?> Handle(GetChecklistConfigByIdQuery request, CancellationToken cancellationToken)
        => _queryService.GetConfigByIdAsync(request.Id, cancellationToken);
}
