namespace TEDF.API.Endpoints.EvaluationChecklists.Requests;

/// <summary>Body for an evaluator saving their checklist result for a project.</summary>
public record SaveProjectChecklistRequest(IReadOnlyList<Guid> PassedCriterionIds, string? Note);

/// <summary>A single editable criterion in a checklist configuration request.</summary>
public record ChecklistCriterionRequest(string TitleVi, string TitleEn, string? Description);

/// <summary>Body for creating a new checklist configuration (Draft) for a semester.</summary>
public record CreateChecklistConfigRequest(int SemesterId, IReadOnlyList<ChecklistCriterionRequest> Criteria);

/// <summary>Body for editing a Draft checklist configuration's criteria.</summary>
public record UpdateChecklistConfigRequest(IReadOnlyList<ChecklistCriterionRequest> Criteria);

/// <summary>Body for copying a checklist configuration into another semester.</summary>
public record CopyChecklistConfigRequest(int TargetSemesterId);
