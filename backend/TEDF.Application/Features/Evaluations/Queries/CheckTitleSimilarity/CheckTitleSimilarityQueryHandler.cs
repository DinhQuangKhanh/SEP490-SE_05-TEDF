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
        // Send the topic's full content to the DASSF analyze endpoint, which compares it against the
        // two most-recent semesters and returns the top matches already enriched with the matched
        // topic's content, the four sub-scores, a revision suggestion, and per-dimension highlights.
        var matches = await _similarityApi.AnalyzeAsync(
            new AnalyzeTopicRequest(
                request.Title,
                request.Description,
                request.Scope,
                request.Objectives,
                request.ExpectedResult,
                request.Technologies),
            cancellationToken);

        return matches.ToList();
    }
}
