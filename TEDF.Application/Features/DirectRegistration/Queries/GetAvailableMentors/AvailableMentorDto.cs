namespace TEDF.Application.Features.DirectRegistration.Queries.GetAvailableMentors;

public record AvailableMentorDto(
    Guid MentorId,
    string FullName,
    string Email,
    string? AcademicTitle,
    int CurrentGroupCount,
    int MaxGroups
);
