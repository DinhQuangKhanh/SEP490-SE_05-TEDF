using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.ExplainSimilarity;

/// <summary>Forwards the two topics' content to the DASSF explain endpoint and returns per-field text.</summary>
public class ExplainSimilarityQueryHandler : IQueryHandler<ExplainSimilarityQuery, List<FieldExplanationDto>>
{
    private readonly ISimilarityApiClient _similarityApi;

    public ExplainSimilarityQueryHandler(ISimilarityApiClient similarityApi)
    {
        _similarityApi = similarityApi;
    }

    public async Task<List<FieldExplanationDto>> Handle(ExplainSimilarityQuery request, CancellationToken cancellationToken)
    {
        var fields = await _similarityApi.ExplainAsync(
            new ExplainTopicRequest(request.Query, request.Match), cancellationToken);
        return fields.ToList();
    }
}
