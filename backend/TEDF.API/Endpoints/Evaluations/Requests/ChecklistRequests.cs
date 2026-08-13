namespace TEDF.API.Endpoints.EvaluationChecklists.Requests;

/// <summary>One evaluator evaluation entry for a criterion.</summary>
public record ChecklistEvaluationItemRequest(Guid CriterionId, bool IsPassed, string? Comment);

/// <summary>Body for an evaluator saving their checklist result for a project.</summary>
public record SaveProjectChecklistRequest(IReadOnlyList<ChecklistEvaluationItemRequest> Items, string? Note);

/// <summary>A single editable criterion in a checklist configuration request.</summary>
public record ChecklistCriterionRequest(
    string TitleVi, string TitleEn, string? Description);

/// <summary>Body for creating a new checklist configuration (Draft) for a semester by manual entry.</summary>
public record CreateChecklistConfigRequest(
    int SemesterId, IReadOnlyList<ChecklistCriterionRequest> Criteria, int RequiredPassCount);

/// <summary>Body for editing a Draft checklist configuration's criteria + required-pass count.</summary>
public record UpdateChecklistConfigRequest(
    IReadOnlyList<ChecklistCriterionRequest> Criteria, int RequiredPassCount);

/// <summary>Body for copying a checklist configuration into another semester.</summary>
public record CopyChecklistConfigRequest(int TargetSemesterId);
