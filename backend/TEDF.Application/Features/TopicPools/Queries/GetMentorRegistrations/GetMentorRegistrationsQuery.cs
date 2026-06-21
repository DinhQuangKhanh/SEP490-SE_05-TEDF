using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetMentorRegistrations;

/// <summary>
/// Pending pool-topic registration requests for the current mentor's topics (newest first).
/// </summary>
public record GetMentorRegistrationsQuery() : IQuery<List<MentorRegistrationRequestDto>>;
