namespace TEDF.Application.Features.StudentGroups.DTOs;

/// <summary>A student eligible to be invited to a group (not yet in an active group this semester).</summary>
public record AvailableStudentDto(
    Guid StudentId,
    string StudentCode,
    string FullName);
