using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Talks to the external Python (DASSF) similarity service. The web system reuses each
/// project's id as the thesis id, so a project and its thesis are always the same entity.
/// </summary>
public interface ISimilarityApiClient
{
    /// <summary>
    /// Registers a topic in the similarity corpus (POST /api/v1/theses), pinning the thesis id
    /// to the project id so later checks line up. Best-effort — implementations should not throw.
    /// </summary>
    Task CreateThesisAsync(CreateThesisRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs (or returns the already-computed) similarity for the given project id
    /// (POST /api/v1/similarity/run-new) and returns each matching pair's score + reasons.
    /// </summary>
    Task<IReadOnlyList<SimilarityMatchDto>> RunSimilarityForNewAsync(Guid thesisId, CancellationToken cancellationToken = default);
}

/// <summary>Payload for registering one topic in the similarity corpus.</summary>
public sealed record CreateThesisRequest(
    Guid ThesisId,
    string Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    string? Semester,
    string? Program,
    IReadOnlyList<string> Domains,
    IReadOnlyList<string> Technologies);
