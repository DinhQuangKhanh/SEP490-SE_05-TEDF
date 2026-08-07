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

/// <summary>
/// One similarity match returned by the Python (DASSF) similarity engine: the overall
/// composite score and the human-readable reasons behind it. See the Python
/// <c>score_calculator</c> — each reason maps to one scoring dimension.
/// </summary>
public record SimilarityMatchDto
{
    /// <summary>Id of the other topic/thesis in the pair (equals its web project id).</summary>
    public Guid OtherThesisId { get; init; }

    /// <summary>MDDM composite score in [0, 1].</summary>
    public double OverallScore { get; init; }

    /// <summary>Level bucket: Low | Moderate | High | Critical.</summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>Explanations, e.g. "same tech stack with a different business domain".</summary>
    public List<string> Reasons { get; init; } = [];

    // ── Matched topic content (populated for the top matches, for the side-by-side view) ──
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Scope { get; init; }
    public string? Objectives { get; init; }
    public string? ExpectedResult { get; init; }
    public string? Semester { get; init; }
    public List<string> Technologies { get; init; } = [];
}

/// <summary>A matched topic's content translated to Vietnamese (on-demand comparison view).</summary>
public record TranslatedThesisDto
{
    public Guid OtherThesisId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Scope { get; init; }
    public string? Objectives { get; init; }
    public string? ExpectedResult { get; init; }
    public List<string> Technologies { get; init; } = [];
    /// <summary>False when the LLM was unavailable and the original text was returned.</summary>
    public bool Translated { get; init; }
}
