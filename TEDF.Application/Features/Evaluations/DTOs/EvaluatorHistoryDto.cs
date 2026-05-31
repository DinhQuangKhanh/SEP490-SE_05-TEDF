namespace TEDF.Application.Features.Evaluations.DTOs;

public record EvaluatorHistoryStatsDto
{
    public int TotalReviewed { get; init; }
    public int ApprovedCount { get; init; }
    public int NeedsModificationCount { get; init; }
    public int RejectedCount { get; init; }
}

public record EvaluatorHistoryItemDto
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectNameVi { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string? StudentAvatar { get; init; }
    public DateTime EvaluatedAt { get; init; }
    public string Result { get; init; } = string.Empty;
    public string? Feedback { get; init; }
}

public record EvaluatorHistoryDto
{
    public EvaluatorHistoryStatsDto Stats { get; init; } = null!;
    public List<EvaluatorHistoryItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
