using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetGroupJoinRequests;

public record GetGroupJoinRequestsQuery(Guid GroupId) : IQuery<List<JoinRequestDto>>;
