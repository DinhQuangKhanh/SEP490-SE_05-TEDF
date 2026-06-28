using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

/// <summary>The student's major is derived server-side from the eligible-student roster.</summary>
public record GetAvailableMentorsQuery() : IQuery<AvailableMentorsResponse>;
