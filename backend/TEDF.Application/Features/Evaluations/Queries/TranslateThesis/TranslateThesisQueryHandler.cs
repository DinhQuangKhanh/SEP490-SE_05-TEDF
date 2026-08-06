using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.TranslateThesis;

public class TranslateThesisQueryHandler : IQueryHandler<TranslateThesisQuery, TranslatedThesisDto>
{
    private readonly ISimilarityApiClient _similarityApi;

    public TranslateThesisQueryHandler(ISimilarityApiClient similarityApi)
    {
        _similarityApi = similarityApi;
    }

    public async Task<TranslatedThesisDto> Handle(TranslateThesisQuery request, CancellationToken cancellationToken)
    {
        var content = await _similarityApi.GetThesisTranslatedAsync(request.ThesisId, cancellationToken);
        return new TranslatedThesisDto
        {
            OtherThesisId = request.ThesisId,
            Title = content?.Title,
            Description = content?.Description,
            Scope = content?.Scope,
            Objectives = content?.Objectives,
            ExpectedResult = content?.ExpectedResult,
            Technologies = content?.Technologies.ToList() ?? [],
            Translated = content?.Translated ?? false,
        };
    }
}
