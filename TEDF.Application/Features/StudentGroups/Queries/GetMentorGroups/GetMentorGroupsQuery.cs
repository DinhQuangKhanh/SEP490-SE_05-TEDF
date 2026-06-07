using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetMentorGroups;

/// <summary>
/// Query to get all groups that the current mentor is guiding in a semester.
/// If SemesterId is null, the active semester is used.
/// </summary>
public record GetMentorGroupsQuery(int? SemesterId) : IQuery<List<MentorGroupDto>>;
