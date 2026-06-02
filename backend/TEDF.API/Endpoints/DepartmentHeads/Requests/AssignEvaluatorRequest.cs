namespace TEDF.API.Endpoints.DepartmentHeads.Requests;

/// <summary>
/// Request body for assigning an evaluator to a project.
/// </summary>
public sealed record AssignEvaluatorRequest(Guid ProjectId, Guid EvaluatorId, int EvaluatorOrder);
