namespace TEDF.Application.Features.Projects.DTOs;

public class DepartmentProjectDto
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string SemesterName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusValue { get; set; }
    public string? SubmittedAt { get; set; }
    public List<EvaluatorAssignmentDto> Evaluators { get; set; } = [];
    public List<MentorSummaryDto> Mentors { get; set; } = [];
    public bool HasConflict { get; set; }
    public bool NeedsFinalDecision { get; set; }
    public int AssignedEvaluatorCount { get; set; }
}

public class EvaluatorAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public int EvaluatorOrder { get; set; }
    public string? IndividualResult { get; set; }
    public int? IndividualResultValue { get; set; }
    public string? Feedback { get; set; }
    public string? EvaluatedAt { get; set; }
    public bool HasSubmitted { get; set; }
}

public class MentorSummaryDto
{
    public Guid MentorId { get; set; }
    public string MentorName { get; set; } = string.Empty;
}

public class DepartmentProjectsResponse
{
    public List<DepartmentProjectDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PendingAssignmentCount { get; set; }
    public int InEvaluationCount { get; set; }
    public int NeedsFinalDecisionCount { get; set; }
    public int CompletedCount { get; set; }
}

public class DepartmentEvaluatorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AcademicTitle { get; set; }
    public int ActiveAssignmentCount { get; set; }
}
