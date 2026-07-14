namespace TEDF.API.Endpoints.Users.Requests;

public record UpdateMyProfileRequest(
    string? PhoneNumber,
    DateOnly? BirthDate,
    string? PrivacySettings
);
