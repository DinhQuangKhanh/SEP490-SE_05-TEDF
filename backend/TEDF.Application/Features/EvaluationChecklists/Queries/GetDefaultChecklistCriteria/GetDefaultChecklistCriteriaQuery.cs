using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetDefaultChecklistCriteria;

/// <summary>Returns the 10 default criteria (backend-sourced) to prefill the create-checklist form.</summary>
public record GetDefaultChecklistCriteriaQuery : ICachedQuery<IReadOnlyList<ChecklistCriterionSeedDto>>
{
    // Static content; shares the checklist-configs: prefix so it is cleared alongside config mutations.
    public string CacheKey => "checklist-configs:default-criteria";
}
