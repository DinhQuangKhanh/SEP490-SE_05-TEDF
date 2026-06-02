using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetEvaluatorProjects;

public record GetEvaluatorProjectsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    int? SemesterId = null,
    int? MajorId = null,
    string? Result = null
) : ICachedQuery<EvaluatorProjectsDto>
{
    // Skip caching when search is active (unique queries = low hit rate + key explosion)
    public string? CacheKey => !string.IsNullOrEmpty(Search)
        ? null
        : $"evaluator:{{userId}}:projects:{Page}:{PageSize}:{SemesterId?.ToString() ?? "_"}:{MajorId?.ToString() ?? "_"}:{Result ?? "_"}";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
