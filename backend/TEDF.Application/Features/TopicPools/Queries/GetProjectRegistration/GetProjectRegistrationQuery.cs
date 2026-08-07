using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetProjectRegistration;

/// <summary>
/// Query returning the confirmed registration for a project — the group that was assigned the topic —
/// so the supervising mentor can view the group's registration note (reason + attachments).
/// Returns null when the project has no confirmed registration (e.g. a direct-registration topic).
/// </summary>
public record GetProjectRegistrationQuery(Guid ProjectId) : IQuery<GroupRegistrationDto?>;
