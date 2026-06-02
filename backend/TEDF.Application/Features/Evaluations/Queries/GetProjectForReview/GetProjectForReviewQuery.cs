using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetProjectForReview;

public record GetProjectForReviewQuery(Guid ProjectId) : IQuery<ProjectReviewDetailDto?>;
