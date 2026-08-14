using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;

/// <summary>Check a topic (its full content) against the two most-recent semesters via DASSF analyze.</summary>
public record CheckTitleSimilarityQuery(
    Guid ProjectId,
    string Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    IReadOnlyList<string> Technologies) : IQuery<List<SimilarityMatchDto>>;
