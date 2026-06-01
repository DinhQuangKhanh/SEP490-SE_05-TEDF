namespace TEDF.Application.Features.Semesters.DTOs;

/// <summary>
/// DTO representing a semester with its phases.
/// </summary>
public record SemesterDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string AcademicYear { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<SemesterPhaseDto> Phases { get; init; } = [];
}

/// <summary>
/// DTO representing a phase within a semester.
/// </summary>
public record SemesterPhaseDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Order { get; init; }
    public string Status { get; init; } = string.Empty;
    public int DurationDays { get; init; }
}
