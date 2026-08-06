using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;

/// <summary>
/// Delegates the duplicate check to the external Python (DASSF) similarity engine, keyed by the
/// project id (which is also the thesis id). Returns each matching pair's overall score + reasons.
/// </summary>
public class CheckTitleSimilarityQueryHandler : IQueryHandler<CheckTitleSimilarityQuery, List<SimilarityMatchDto>>
{
    private readonly ISimilarityApiClient _similarityApi;

    public CheckTitleSimilarityQueryHandler(ISimilarityApiClient similarityApi)
    {
        _similarityApi = similarityApi;
    }

    public async Task<List<SimilarityMatchDto>> Handle(CheckTitleSimilarityQuery request, CancellationToken cancellationToken)
    {
        var matches = await _similarityApi.RunSimilarityForNewAsync(request.ProjectId, cancellationToken);
        return matches.ToList();
    }
}
