namespace TEDF.Application.Features.Dashboard.DTOs;

public record AdminDashboardDto
{
    public AdminStatsDto Stats { get; init; } = null!;
    public SemesterProgressDto? SemesterProgress { get; init; }
    public ApprovalRateDto ApprovalRate { get; init; } = null!;
    public List<RecentTicketDto> RecentTickets { get; init; } = [];
}

public record AdminStatsDto
{
    public int TotalStudents { get; init; }
    public int TotalMentors { get; init; }
    public int TotalRegisteredTopics { get; init; }
    public int HighPriorityPending { get; init; }
}

public record SemesterProgressDto
{
    public string SemesterName { get; init; } = string.Empty;
    public List<SemesterPhaseDto> Phases { get; init; } = [];
}

public record SemesterPhaseDto
{
    public string Name { get; init; } = string.Empty;
    public int Type { get; init; }
    public int Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int Order { get; init; }
}

public record ApprovalRateDto
{
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int InProgress { get; init; }
    public int Pending { get; init; }
    public int Total { get; init; }
}

public record RecentTicketDto
{
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ReporterName { get; init; } = string.Empty;
    public int Category { get; init; }
    public int Priority { get; init; }
    public int Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
