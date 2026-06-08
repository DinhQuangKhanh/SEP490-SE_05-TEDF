using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetInvitableStudents;

/// <summary>Students that the leader can invite to the given group (not yet in a group this semester).</summary>
public record GetInvitableStudentsQuery(Guid GroupId) : IQuery<List<AvailableStudentDto>>;
