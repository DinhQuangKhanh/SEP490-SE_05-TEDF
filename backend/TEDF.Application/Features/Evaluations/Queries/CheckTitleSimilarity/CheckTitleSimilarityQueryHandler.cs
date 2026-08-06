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

        // Only the top 5 matter to a reviewer. Enrich each with the matched topic's content
        // (fetched from the similarity service) so the UI can show it side-by-side and highlight
        // the overlapping text.
        var enriched = await Task.WhenAll(matches.Take(5).Select(async match =>
        {
            var content = await _similarityApi.GetThesisAsync(match.OtherThesisId, cancellationToken);
            if (content is null) return match;

            return match with
            {
                Title = content.Title,
                Description = content.Description,
                Scope = content.Scope,
                Objectives = content.Objectives,
                ExpectedResult = content.ExpectedResult,
                Semester = content.Semester,
                Technologies = content.Technologies.ToList(),
            };
        }));

        return enriched.ToList();
    }
}
