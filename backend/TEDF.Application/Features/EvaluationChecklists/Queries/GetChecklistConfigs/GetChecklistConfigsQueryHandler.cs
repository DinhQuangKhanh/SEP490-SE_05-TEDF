using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigs;

public class GetChecklistConfigsQueryHandler : IQueryHandler<GetChecklistConfigsQuery, ChecklistConfigListDto>
{
    private readonly IChecklistQueryService _queryService;

    public GetChecklistConfigsQueryHandler(IChecklistQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<ChecklistConfigListDto> Handle(GetChecklistConfigsQuery request, CancellationToken cancellationToken)
        => _queryService.GetConfigsAsync(request.SemesterId, cancellationToken);
}
