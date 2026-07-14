using TEDF.Application.Common.Abstractions;

namespace TEDF.Application.Features.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    string? PhoneNumber,
    DateOnly? BirthDate,
    string? PrivacySettings
) : ICommand;
