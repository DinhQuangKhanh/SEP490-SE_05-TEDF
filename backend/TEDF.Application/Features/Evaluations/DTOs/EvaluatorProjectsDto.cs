namespace TEDF.Application.Features.Evaluations.DTOs;

public record EvaluatorProjectItemDto
{
    public Guid AssignmentId { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string ProjectNameVi { get; init; } = string.Empty;
    public string MajorName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string? StudentAvatar { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public DateTime? SubmittedAt { get; init; }
    public DateTime AssignedAt { get; init; }
    public string IndividualResult { get; init; } = string.Empty;
    public bool IsUrgent { get; init; }
}

public record EvaluatorProjectsDto
{
    public List<EvaluatorProjectItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
