using System.Security.Claims;
using TEDF.Application.Common;

namespace TEDF.API.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to simplify user identity retrieval.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the database user ID (Guid) from the ClaimsPrincipal.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(AppClaimTypes.DbUserId)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("User ID claim not found or invalid.");
    }
}
