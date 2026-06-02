namespace TEDF.API.Endpoints.DepartmentHeads.Requests;

public record AssignEvaluatorRequest(Guid ProjectId, Guid EvaluatorId, int EvaluatorOrder);
public record SubmitFinalDecisionRequest(int Result, string? Notes);
