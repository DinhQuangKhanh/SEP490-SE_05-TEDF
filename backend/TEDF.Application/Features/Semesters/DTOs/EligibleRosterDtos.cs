namespace TEDF.Application.Features.Semesters.DTOs;

/// <summary>A row in a semester's eligible-student roster (for the admin preview table).</summary>
public record EligibleStudentDto
{
    public Guid StudentId { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public int? MajorId { get; init; }
    public string? ProgramCode { get; init; }
    public string? ProgramName { get; init; }
    public bool IsEligible { get; init; }
    public DateTime ImportedAt { get; init; }
}

/// <summary>A row in a semester's eligible-mentor roster (for the admin preview table).</summary>
public record EligibleMentorDto
{
    public Guid MentorId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    /// <summary>Bộ môn giảng viên đang dạy (SE/CF/AI/IA…), snapshot từ file import.</summary>
    public string? Division { get; init; }
    public int? MajorId { get; init; }
    public string? ProgramCode { get; init; }
    public string? ProgramName { get; init; }
    public bool IsAssigned { get; init; }
    public DateTime ImportedAt { get; init; }
}
