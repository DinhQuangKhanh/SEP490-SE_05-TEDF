namespace TEDF.Application.Features.Dashboard.DTOs;

public record MentorDashboardDto
{
    public string MentorName { get; init; } = "";
    public MentorStatsDto Stats { get; init; } = null!;
    public SemesterProgressDto? SemesterProgress { get; init; }
    public List<RecentProjectDto> RecentProjects { get; init; } = [];
}

public record MentorStatsDto
{
    public int TotalGroups { get; init; }
    public int TotalStudents { get; init; }
    public int PendingEvaluation { get; init; }
    public int ApprovedProjects { get; init; }
    public int InProgressProjects { get; init; }
    public int TotalProjects { get; init; }
}

public record RecentProjectDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameVi { get; init; } = "";
    public string NameEn { get; init; } = "";
    public int Status { get; init; }
    public int SourceType { get; init; }
    public string? GroupName { get; init; }
    public string? LeaderName { get; init; }
    public int MemberCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
}
