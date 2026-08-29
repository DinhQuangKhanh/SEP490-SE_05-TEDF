using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Handles ProposeTopicToPoolCommand by delegating to <see cref="ITopicProposalService"/>,
/// then registers the new topic in the Python similarity corpus using the project's id as the
/// thesis id (so later duplicate checks line up).
/// </summary>
public class ProposeTopicToPoolCommandHandler : ICommandHandler<ProposeTopicToPoolCommand, Guid>
{
    private readonly ITopicProposalService _topicPools;
    private readonly ICurrentUserService _currentUser;
    private readonly ISimilarityApiClient _similarityApi;

    public ProposeTopicToPoolCommandHandler(
        ITopicProposalService topicPools,
        ICurrentUserService currentUser,
        ISimilarityApiClient similarityApi)
    {
        _topicPools = topicPools;
        _currentUser = currentUser;
        _similarityApi = similarityApi;
    }

    public async Task<Guid> Handle(ProposeTopicToPoolCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // The domain service parses the uploaded form, validates it (Kinds-of-person / the logged-in
        // lecturer must be the mentor named on the form / 3.1–3.4), maps it onto the project and returns
        // the mapped content for the corpus below.
        var (projectId, content) = await _topicPools.ProposeTopicFromFormAsync(
            request.PoolId, request.RegisterForm, request.Note, _currentUser.UserId.Value, cancellationToken);

        // Register the topic in the similarity corpus under the SAME id, so a later "check duplicates"
        // finds it. Best-effort inside the client — a failure here never fails the proposal.
        await _similarityApi.CreateThesisAsync(
            new CreateThesisRequest(
                ThesisId: projectId,
                Title: string.IsNullOrWhiteSpace(content.NameEn) ? content.NameVi : content.NameEn,
                Description: content.Description,
                Scope: content.Scope,
                Objectives: content.Objectives,
                ExpectedResult: content.ExpectedResults,
                Semester: null,
                Program: null,
                Domains: [],
                Technologies: SplitCsv(content.Technologies)),
            cancellationToken);

        return projectId;
    }

    /// <summary>Splits a comma-separated technologies string into a trimmed, non-empty list.</summary>
    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
