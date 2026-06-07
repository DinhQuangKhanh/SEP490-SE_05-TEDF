using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetMyInvitations;

public record GetMyInvitationsQuery : IQuery<List<InvitationDto>>;
