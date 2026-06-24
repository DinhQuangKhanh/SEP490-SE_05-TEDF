namespace TEDF.Application.Features.Authentications.DTOs;

/// <summary>Session bootstrap info the SPA fetches once after Firebase sign-in.</summary>
public record SessionDto(
    Guid UserId,
    string FullName,
    string Email,
    List<string> Roles,
    string Status,
    SessionAccessDto Access);

/// <summary>Whether the account may use the system. Kind: null | "locked" | "inactive" | "student_not_eligible".</summary>
public record SessionAccessDto(bool Allowed, string? Kind, string? Reason);
