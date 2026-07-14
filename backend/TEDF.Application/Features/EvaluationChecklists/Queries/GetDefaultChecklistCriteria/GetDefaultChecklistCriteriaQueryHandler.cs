using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetDefaultChecklistCriteria;

public class GetDefaultChecklistCriteriaQueryHandler
    : IQueryHandler<GetDefaultChecklistCriteriaQuery, IReadOnlyList<ChecklistCriterionSeedDto>>
{
    public Task<IReadOnlyList<ChecklistCriterionSeedDto>> Handle(
        GetDefaultChecklistCriteriaQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ChecklistCriterionSeedDto> result = DefaultChecklistCriteria.Items
            .Select((c, i) => new ChecklistCriterionSeedDto(i + 1, c.TitleVi, c.TitleEn, c.Description))
            .ToList();

        return Task.FromResult(result);
    }
}
