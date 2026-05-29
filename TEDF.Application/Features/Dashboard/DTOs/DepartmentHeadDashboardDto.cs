namespace TEDF.Application.Features.Dashboard.DTOs;

public record DepartmentHeadDashboardDto
{
    public string DepartmentName { get; init; } = "";
    public string HeadName { get; init; } = "";
    public DepartmentHeadStatsDto Stats { get; init; } = null!;
    public SemesterProgressDto? SemesterProgress { get; init; }
    public EvaluationProgressDto EvaluationProgress { get; init; } = null!;
    public List<RecentEvaluationActivityDto> RecentActivities { get; init; } = [];
}

public record DepartmentHeadStatsDto
{
    public int TotalProjects { get; init; }
    public int PendingAssignment { get; init; }
    public int InEvaluation { get; init; }
    public int NeedsFinalDecision { get; init; }
    public int Completed { get; init; }
    public int TotalEvaluators { get; init; }
    public int TotalMentors { get; init; }
}

public record EvaluationProgressDto
{
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int NeedsModification { get; init; }
    public int Pending { get; init; }
}

public record RecentEvaluationActivityDto
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = "";
    public string ProjectName { get; init; } = "";
    public string ActivityType { get; init; } = "";
    public string ActorName { get; init; } = "";
    public DateTime OccurredAt { get; init; }
}
