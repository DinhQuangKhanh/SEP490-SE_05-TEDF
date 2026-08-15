using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.ExplainSimilarity;

/// <summary>
/// Explain (per field) why a topic under review overlaps one matched topic — grounded in the same
/// highlight spans DASSF computes, so the narrative can never disagree with what the UI paints.
/// </summary>
public record ExplainSimilarityQuery(
    TopicContentPayload Query,
    TopicContentPayload Match) : IQuery<List<FieldExplanationDto>>;
