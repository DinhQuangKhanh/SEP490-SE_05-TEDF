using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetGroupRegistrations;

/// <summary>
/// Query returning all topic-pool registrations made by a student group (newest first),
/// so the group can track a pending/rejected registration.
/// </summary>
public record GetGroupRegistrationsQuery(Guid GroupId) : IQuery<List<GroupRegistrationDto>>;
