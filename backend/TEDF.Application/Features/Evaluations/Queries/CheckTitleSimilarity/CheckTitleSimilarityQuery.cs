using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.CheckTitleSimilarity;

public record CheckTitleSimilarityQuery(Guid ProjectId) : IQuery<List<SimilarityMatchDto>>;
