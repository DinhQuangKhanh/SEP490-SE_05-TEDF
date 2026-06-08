using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetMyPendingJoinRequest;

public record GetMyPendingJoinRequestQuery(int? SemesterId) : IQuery<PendingJoinRequestDto?>;
