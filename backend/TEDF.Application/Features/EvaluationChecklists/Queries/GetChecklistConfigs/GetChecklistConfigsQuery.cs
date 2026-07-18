using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigs;

/// <summary>Lists checklist configurations (optionally filtered by semester) plus the semester options.</summary>
public record GetChecklistConfigsQuery(int? SemesterId) : ICachedQuery<ChecklistConfigListDto>
{
    // Keyed by semester so each filtered list caches independently; invalidated by the checklist-configs: prefix.
    public string CacheKey => $"checklist-configs:list:{SemesterId?.ToString() ?? "all"}";
}
