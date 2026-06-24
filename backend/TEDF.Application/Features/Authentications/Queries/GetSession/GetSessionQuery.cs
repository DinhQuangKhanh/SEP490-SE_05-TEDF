using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Authentications.DTOs;

namespace TEDF.Application.Features.Authentications.Queries.GetSession;

/// <summary>Returns the current user's profile + whether they may use the system.</summary>
public record GetSessionQuery() : IQuery<SessionDto>;
