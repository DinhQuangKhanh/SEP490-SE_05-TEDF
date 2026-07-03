using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Users.DTOs;

namespace TEDF.Application.Features.Users.Queries.GetMyProfile;

/// <summary>
/// Query to retrieve the authenticated user's own profile.
/// </summary>
public record GetMyProfileQuery() : IQuery<MyProfileDto>;
