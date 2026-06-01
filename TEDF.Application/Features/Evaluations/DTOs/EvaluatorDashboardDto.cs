namespace TEDF.Application.Features.Evaluations.DTOs;

public record EvaluatorDashboardDto
{
    public EvaluatorStatsDto Stats { get; init; } = null!;
    public List<PendingEvaluationDto> PendingEvaluations { get; init; } = [];
    public List<RecentReviewedDto> RecentReviewed { get; init; } = [];
}

public record EvaluatorStatsDto
{
    public int TotalAssigned { get; init; }
    public int PendingCount { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
    public int NeedsModificationCount { get; init; }
    public int ReviewedCount { get; init; }
    public double? AvgReviewDays { get; init; }
}

public record PendingEvaluationDto
{
    public Guid AssignmentId { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectNameVi { get; init; } = string.Empty;
    public string MajorName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string? StudentAvatar { get; init; }
    public DateTime AssignedAt { get; init; }
    public int DaysElapsed { get; init; }
    public bool IsUrgent { get; init; }
}

public record RecentReviewedDto
{
    public Guid ProjectId { get; init; }
    public string ProjectNameVi { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public DateTime EvaluatedAt { get; init; }
}
