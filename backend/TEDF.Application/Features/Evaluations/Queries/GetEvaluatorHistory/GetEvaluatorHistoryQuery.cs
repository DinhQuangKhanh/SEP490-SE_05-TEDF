using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetEvaluatorHistory;

public record GetEvaluatorHistoryQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Result = null,
    string? DateRange = null
) : ICachedQuery<EvaluatorHistoryDto>
{
    // Skip caching when search is active (unique queries = low hit rate + key explosion)
    public string? CacheKey => !string.IsNullOrEmpty(Search)
        ? null
        : $"evaluator:{{userId}}:history:{Page}:{PageSize}:{Result ?? "_"}:{DateRange ?? "_"}";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(5);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(15);
}
