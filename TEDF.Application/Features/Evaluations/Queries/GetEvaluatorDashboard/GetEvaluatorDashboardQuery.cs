using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetEvaluatorDashboard;

public record GetEvaluatorDashboardQuery() : ICachedQuery<EvaluatorDashboardDto>
{
    public string CacheKey => "evaluator:{userId}:dashboard";
    public TimeSpan? L1Expiration => TimeSpan.FromMinutes(2);
    public TimeSpan? L2Expiration => TimeSpan.FromMinutes(10);
}
