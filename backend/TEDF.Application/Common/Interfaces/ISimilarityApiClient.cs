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

    /// <summary>
    /// Fetches a single topic's content (GET /api/v1/theses/{id}) — used to show a matched topic
    /// side-by-side with the one under review. Returns null if it can't be fetched.
    /// </summary>
    Task<ThesisContentResult?> GetThesisAsync(Guid thesisId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the full DASSF pipeline on a topic's content against the two most-recent semesters
    /// (POST /api/v1/similarity/analyze) and returns the top matches — each with the four sub-scores,
    /// the matched topic's content, a revision suggestion, and per-dimension highlight spans.
    /// </summary>
    Task<IReadOnlyList<SimilarityMatchDto>> AnalyzeAsync(AnalyzeTopicRequest request, CancellationToken cancellationToken = default);
}

/// <summary>The topic-under-review's content sent to the DASSF analyze endpoint.</summary>
public sealed record AnalyzeTopicRequest(
    string Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    IReadOnlyList<string> Technologies);

/// <summary>The content of a topic in the similarity corpus, for the comparison view.</summary>
public sealed record ThesisContentResult(
    string? Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    string? Semester,
    string? Program,
    IReadOnlyList<string> Technologies,
    bool Translated = false);

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
