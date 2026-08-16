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

    public async Task<IReadOnlyList<SimilarityMatchDto>> AnalyzeAsync(AnalyzeTopicRequest request, CancellationToken cancellationToken = default)
    {
        var body = new AnalyzeBody(
            request.Title, request.Description, request.Scope, request.Objectives,
            request.ExpectedResult, request.Technologies);

        using var response = await _http.PostAsJsonAsync("/api/v1/similarity/analyze", body, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<AnalyzeData>>(JsonOptions, cancellationToken);
        var top = envelope?.Data?.TopMatches ?? [];
        return top.Select(MapMatch).ToList();
    }

    private static SimilarityMatchDto MapMatch(MatchItem m) => new()
    {
        OtherThesisId = Guid.Empty,   // corpus topics come from the JSON, not the web DB
        OverallScore = m.OverallScore,
        Level = m.Level ?? string.Empty,
        Action = m.Action,
        IsStructuralDuplication = m.IsStructuralDuplication,
        Reasons = m.Reasons ?? [],
        Breakdown = m.Breakdown is null ? null
            : new DimensionBreakdownDto(m.Breakdown.Semantic, m.Breakdown.Lexical, m.Breakdown.Structure, m.Breakdown.Domain),
        Title = m.Other?.Title,
        Description = m.Other?.Description,
        Scope = m.Other?.Scope,
        Objectives = m.Other?.Objectives,
        ExpectedResult = m.Other?.ExpectedResult,
        Semester = m.OtherSemester,
        Technologies = m.Other?.Technologies ?? [],
        Highlights = m.Highlights is null ? null : new SimilarityHighlightsDto
        {
            Fields = (m.Highlights.Fields ?? []).Select(MapField).ToList(),
        },
    };

    private static FieldHighlightDto MapField(FieldHighlightItem f) => new()
    {
        Field = f.Field ?? string.Empty,
        Angle = f.Angle ?? string.Empty,
        Score = f.Score,
        A = (f.A ?? []).Select(MapSpan).ToList(),
        B = (f.B ?? []).Select(MapSpan).ToList(),
    };

    private static HighlightSpanDto MapSpan(SpanItem s) => new(s.Text ?? string.Empty, s.Angle ?? string.Empty);

    public async Task<IReadOnlyList<FieldExplanationDto>> ExplainAsync(ExplainTopicRequest request, CancellationToken cancellationToken = default)
    {
        var body = new ExplainBody(ToTopicBody(request.Query), ToTopicBody(request.Match));

        using var response = await _http.PostAsJsonAsync("/api/v1/similarity/explain", body, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<ExplainData>>(JsonOptions, cancellationToken);
        var fields = envelope?.Data?.Fields ?? [];
        return fields
            .Select(f => new FieldExplanationDto(f.Field ?? string.Empty, f.Angle, f.Score, f.Explanation ?? string.Empty))
            .ToList();
    }

    private static TopicBody ToTopicBody(TopicContentPayload p) =>
        new(p.Title, p.Description, p.Scope, p.Objectives, p.ExpectedResult, p.Technologies);

    // ── Wire shapes ─────────────────────────────────────────────────────────────

    private sealed record ExplainBody(
        [property: JsonPropertyName("query")] TopicBody Query,
        [property: JsonPropertyName("match")] TopicBody Match);

    private sealed record TopicBody(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("objectives")] string? Objectives,
        [property: JsonPropertyName("expected_result")] string? ExpectedResult,
        [property: JsonPropertyName("technologies")] IReadOnlyList<string> Technologies);

    private sealed record ExplainData(
        [property: JsonPropertyName("fields")] List<FieldExplanationItem>? Fields);

    private sealed record FieldExplanationItem(
        [property: JsonPropertyName("field")] string? Field,
        [property: JsonPropertyName("angle")] string? Angle,
        [property: JsonPropertyName("score")] double? Score,
        [property: JsonPropertyName("explanation")] string? Explanation);

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

    // ── analyze wire shapes ─────────────────────────────────────────────────────
    private sealed record AnalyzeBody(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("objectives")] string? Objectives,
        [property: JsonPropertyName("expected_result")] string? ExpectedResult,
        [property: JsonPropertyName("technologies")] IReadOnlyList<string> Technologies);

    private sealed record AnalyzeData(
        [property: JsonPropertyName("topMatches")] List<MatchItem>? TopMatches);

    private sealed record MatchItem(
        [property: JsonPropertyName("overall_score")] double OverallScore,
        [property: JsonPropertyName("level")] string? Level,
        [property: JsonPropertyName("action")] string? Action,
        [property: JsonPropertyName("is_structural_duplication")] bool IsStructuralDuplication,
        [property: JsonPropertyName("reasons")] List<string>? Reasons,
        [property: JsonPropertyName("breakdown")] BreakdownItem? Breakdown,
        [property: JsonPropertyName("otherSemester")] string? OtherSemester,
        [property: JsonPropertyName("other")] OtherContent? Other,
        [property: JsonPropertyName("highlights")] HighlightsItem? Highlights);

    private sealed record BreakdownItem(
        [property: JsonPropertyName("semantic")] double Semantic,
        [property: JsonPropertyName("lexical")] double Lexical,
        [property: JsonPropertyName("structure")] double Structure,
        [property: JsonPropertyName("domain")] double Domain);

    private sealed record OtherContent(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("objectives")] string? Objectives,
        [property: JsonPropertyName("expected_result")] string? ExpectedResult,
        [property: JsonPropertyName("technologies")] List<string>? Technologies);

    private sealed record HighlightsItem(
        [property: JsonPropertyName("fields")] List<FieldHighlightItem>? Fields);

    private sealed record FieldHighlightItem(
        [property: JsonPropertyName("field")] string? Field,
        [property: JsonPropertyName("angle")] string? Angle,
        [property: JsonPropertyName("score")] double? Score,
        [property: JsonPropertyName("a")] List<SpanItem>? A,
        [property: JsonPropertyName("b")] List<SpanItem>? B);

    private sealed record SpanItem(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("angle")] string? Angle);
}
