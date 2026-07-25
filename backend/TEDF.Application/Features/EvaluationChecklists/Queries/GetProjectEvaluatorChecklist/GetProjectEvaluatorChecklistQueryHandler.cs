using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectEvaluatorChecklist;

public class GetProjectEvaluatorChecklistQueryHandler
    : IQueryHandler<GetProjectEvaluatorChecklistQuery, ProjectChecklistDto>
{
    private readonly IChecklistQueryService _queryService;

    public GetProjectEvaluatorChecklistQueryHandler(IChecklistQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ProjectChecklistDto> Handle(
        GetProjectEvaluatorChecklistQuery request, CancellationToken cancellationToken)
    {
        // Reuses the same read path as the evaluator's own view — it returns the given evaluator's
        // checklist (saved snapshot, or the active config's initial view when not yet saved).
        return await _queryService.GetProjectChecklistAsync(request.ProjectId, request.EvaluatorId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Người thẩm định này không được gán cho đề tài.");
    }
}
