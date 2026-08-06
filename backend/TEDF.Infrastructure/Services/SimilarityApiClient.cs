using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Infrastructure.Services;

/// <summary>
/// HTTP client for the Python DASSF similarity service (see <see cref="ISimilarityApiClient"/>).
/// The base address is configured via <c>SimilarityService:BaseUrl</c> and injected by the typed
/// <c>AddHttpClient</c> registration.
/// </summary>
public class SimilarityApiClient : ISimilarityApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SimilarityApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public SimilarityApiClient(HttpClient http, ILogger<SimilarityApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task CreateThesisAsync(CreateThesisRequest request, CancellationToken cancellationToken = default)
    {
        // Best-effort: registering a topic in the AI corpus must never block/fail the topic proposal.
        try
        {
            var body = new CreateThesisBody(
                request.ThesisId, request.Title, request.Description, request.Scope, request.Objectives,
                request.ExpectedResult, request.Semester, request.Program, request.Domains, request.Technologies);

            using var response = await _http.PostAsJsonAsync("/api/v1/theses", body, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // 409 = the corpus already has an identical topic; anything else is a genuine problem.
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Similarity create_thesis for {ThesisId} returned {Status}: {Payload}",
                    request.ThesisId, (int)response.StatusCode, payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not register thesis {ThesisId} in the similarity service.", request.ThesisId);
        }
    }

    public async Task<IReadOnlyList<SimilarityMatchDto>> RunSimilarityForNewAsync(Guid thesisId, CancellationToken cancellationToken = default)
    {
        // Body is a bare JSON array of ids: run-new scores this thesis against the whole corpus
        // (and returns the already-stored pairs on subsequent calls).
        using var response = await _http.PostAsJsonAsync("/api/v1/similarity/run-new", new[] { thesisId }, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<RunNewData>>(JsonOptions, cancellationToken);
        var similarities = envelope?.Data?.Similarities ?? [];

        return similarities
            .Select(s => new SimilarityMatchDto
            {
                // The pair is unordered (a/b sorted); surface whichever side isn't the queried thesis.
                OtherThesisId = s.ThesisAId == thesisId ? s.ThesisBId : s.ThesisAId,
                OverallScore = s.OverallScore,
                Level = s.Level ?? string.Empty,
                Reasons = s.Reason ?? [],
            })
            .OrderByDescending(m => m.OverallScore)
            .ToList();
    }

    public async Task<ThesisContentResult?> GetThesisAsync(Guid thesisId, CancellationToken cancellationToken = default)
    {
        try
        {
            var envelope = await _http.GetFromJsonAsync<Envelope<ThesisDetailData>>(
                $"/api/v1/theses/{thesisId}", JsonOptions, cancellationToken);
            var d = envelope?.Data;
            if (d is null) return null;

            return new ThesisContentResult(
                d.Title, d.Description, d.Scope, d.Objectives, d.ExpectedResult, d.Semester, d.Program,
                d.Technologies ?? new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch thesis {ThesisId} content from the similarity service.", thesisId);
            return null;
        }
    }

    // ── Wire shapes ─────────────────────────────────────────────────────────────

    private sealed record CreateThesisBody(
        [property: JsonPropertyName("thesis_id")] Guid ThesisId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("objectives")] string? Objectives,
        [property: JsonPropertyName("expected_result")] string? ExpectedResult,
        [property: JsonPropertyName("semester")] string? Semester,
        [property: JsonPropertyName("program")] string? Program,
        [property: JsonPropertyName("domains")] IReadOnlyList<string> Domains,
        [property: JsonPropertyName("technologies")] IReadOnlyList<string> Technologies);

    private sealed record Envelope<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] T? Data);

    private sealed record RunNewData(
        [property: JsonPropertyName("similarities")] List<SimItem>? Similarities);

    private sealed record SimItem(
        [property: JsonPropertyName("thesis_a_id")] Guid ThesisAId,
        [property: JsonPropertyName("thesis_b_id")] Guid ThesisBId,
        [property: JsonPropertyName("overall_score")] double OverallScore,
        [property: JsonPropertyName("level")] string? Level,
        [property: JsonPropertyName("reason")] List<string>? Reason);

    private sealed record ThesisDetailData(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("objectives")] string? Objectives,
        [property: JsonPropertyName("expected_result")] string? ExpectedResult,
        [property: JsonPropertyName("semester")] string? Semester,
        [property: JsonPropertyName("program")] string? Program,
        [property: JsonPropertyName("technologies")] List<string>? Technologies);
}
