namespace TEDF.Application.Features.Evaluations.DTOs;

public record ProjectReviewDetailDto
{
    // Project info
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string NameVi { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? NameAbbr { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Objectives { get; init; } = string.Empty;
    public string? Scope { get; init; }
    public string? Technologies { get; init; }
    public string? ExpectedResults { get; init; }
    public int MaxStudents { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public int EvaluationCount { get; init; }

    // Major
    public string MajorName { get; init; } = string.Empty;
    public string MajorCode { get; init; } = string.Empty;

    // Semester
    public string SemesterName { get; init; } = string.Empty;

    // People
    public string MentorName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string? StudentAvatar { get; init; }

    // Assignment info
    public Guid AssignmentId { get; init; }
    public DateTime AssignedAt { get; init; }
    public int DaysElapsed { get; init; }
    public string? ExistingFeedback { get; init; }
    public string? ExistingResult { get; init; }
}

public record SimilarTitleDto
{
    public Guid ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string NameVi { get; init; } = string.Empty;
    public string SemesterName { get; init; } = string.Empty;
    public double Similarity { get; init; }
    public List<string> CommonKeywords { get; init; } = [];

    // For comparison panel
    public string Description { get; init; } = string.Empty;
    public string Objectives { get; init; } = string.Empty;
    public string? Scope { get; init; }
    public string? Technologies { get; init; }
    public string? ExpectedResults { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
}
