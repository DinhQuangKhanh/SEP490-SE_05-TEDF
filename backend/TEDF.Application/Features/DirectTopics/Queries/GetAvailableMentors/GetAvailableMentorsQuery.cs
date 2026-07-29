using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

/// <summary>
/// The student's major is derived server-side from the eligible-student roster. Mentor group counts
/// are computed for the registering group's own semester (the thesis term the topic will belong to).
/// </summary>
public record GetAvailableMentorsQuery(Guid GroupId) : IQuery<AvailableMentorsResponse>;
