namespace TEDF.API.Endpoints.Evaluations.Requests;

public record SubmitEvaluationRequest(int Result, string? Feedback);

// Department-head evaluation-management actions (moved here from the role-based DepartmentHead folder).
public record AssignEvaluatorRequest(Guid ProjectId, int PhaseId, Guid EvaluatorId, int EvaluatorOrder);
public record SubmitFinalDecisionRequest(int Result, string? Notes);
