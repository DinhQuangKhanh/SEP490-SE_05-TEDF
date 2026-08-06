using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;

/// <summary>
/// Handles ProposeTopicToPoolCommand by delegating to <see cref="ITopicPoolsDomainService"/>,
/// then registers the new topic in the Python similarity corpus using the project's id as the
/// thesis id (so later duplicate checks line up).
/// </summary>
public class ProposeTopicToPoolCommandHandler : ICommandHandler<ProposeTopicToPoolCommand, Guid>
{
    private readonly ITopicPoolsDomainService _topicPools;
    private readonly ICurrentUserService _currentUser;
    private readonly ISimilarityApiClient _similarityApi;

    public ProposeTopicToPoolCommandHandler(
        ITopicPoolsDomainService topicPools,
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

        var projectId = await _topicPools.ProposeTopicAsync(
            request.PoolId,
            _currentUser.UserId.Value,
            new PoolTopicContent(
                request.NameVi, request.NameEn, request.NameAbbr, request.Description,
                request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents),
            request.RegisterFormPdf,
            cancellationToken);

        // Register the topic in the similarity corpus under the SAME id, so a later "check duplicates"
        // finds it. Best-effort inside the client — a failure here never fails the proposal.
        await _similarityApi.CreateThesisAsync(
            new CreateThesisRequest(
                ThesisId: projectId,
                Title: string.IsNullOrWhiteSpace(request.NameEn) ? request.NameVi : request.NameEn,
                Description: request.Description,
                Scope: request.Scope,
                Objectives: request.Objectives,
                ExpectedResult: request.ExpectedResults,
                Semester: null,
                Program: null,
                Domains: [],
                Technologies: SplitCsv(request.Technologies)),
            cancellationToken);

        return projectId;
    }

    /// <summary>Splits a comma-separated technologies string into a trimmed, non-empty list.</summary>
    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
