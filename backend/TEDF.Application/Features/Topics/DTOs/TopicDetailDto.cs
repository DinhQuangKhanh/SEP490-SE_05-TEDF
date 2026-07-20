namespace TEDF.Application.Features.Topics.DTOs;

/// <summary>
/// Full detail DTO for a single thesis topic (project).
/// Works for all source types: FromPool and DirectRegistration.
/// Includes all fields needed for the topic detail view.
/// </summary>
public class TopicDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string NameVi { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string NameAbbr { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Objectives { get; init; } = string.Empty;
    public string? Scope { get; init; }
    public string? Technologies { get; init; }
    public string? ExpectedResults { get; init; }
    public int MajorId { get; init; }
    public string MajorName { get; init; } = string.Empty;
    public string MajorCode { get; init; } = string.Empty;
    public int PoolStatus { get; init; }
    public string PoolStatusName { get; init; } = string.Empty;
    public int MaxStudents { get; init; }
    public List<MentorSummaryDto> Mentors { get; init; } = [];
    /// <summary>Mentor's latest modification-request note (set when status is NeedsModification), shown to the student.</summary>
    public string? MentorFeedback { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Lightweight mentor info for topic detail view.
/// </summary>
public class MentorSummaryDto
{
    public Guid MentorId { get; init; }
    public string FullName { get; init; } = string.Empty;
}
