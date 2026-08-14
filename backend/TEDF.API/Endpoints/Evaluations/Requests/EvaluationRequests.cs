namespace TEDF.API.Endpoints.Evaluations.Requests;

public record SubmitEvaluationRequest(int Result, string? Feedback);

/// <summary>The topic-under-review's content, sent to the DASSF analyze endpoint for duplicate check.</summary>
public record CheckSimilarityRequest(
    string Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    List<string>? Technologies);

/// <summary>One topic's comparable content for the explain call (title = English title only).</summary>
public record TopicContentRequest(
    string? Title,
    string? Description,
    string? Scope,
    string? Objectives,
    string? ExpectedResult,
    List<string>? Technologies);

/// <summary>The two topics (under review + a matched one) to explain the field-level overlap between.</summary>
public record ExplainSimilarityRequest(TopicContentRequest Query, TopicContentRequest Match);

// Department-head evaluation-management actions (moved here from the role-based DepartmentHead folder).
public record AssignEvaluatorRequest(Guid ProjectId, int PhaseId, Guid EvaluatorId, int EvaluatorOrder);
public record SubmitFinalDecisionRequest(int Result, string? Notes);
