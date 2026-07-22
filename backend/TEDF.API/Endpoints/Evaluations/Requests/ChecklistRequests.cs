namespace TEDF.API.Endpoints.EvaluationChecklists.Requests;

/// <summary>One evaluator score entry for a criterion (score null = not yet scored).</summary>
public record ChecklistScoreItemRequest(Guid CriterionId, decimal? Score, string? Comment);

/// <summary>Body for an evaluator saving their checklist result (scores + per-criterion comments) for a project.</summary>
public record SaveProjectChecklistRequest(IReadOnlyList<ChecklistScoreItemRequest> Items, string? Note);

/// <summary>A single editable criterion (with its scoring bounds) in a checklist configuration request.</summary>
public record ChecklistCriterionRequest(
    string TitleVi, string TitleEn, string? Description, decimal MaxScore, decimal PassScore);

/// <summary>Body for creating a new checklist configuration (Draft) for a semester by manual entry.</summary>
public record CreateChecklistConfigRequest(
    int SemesterId, IReadOnlyList<ChecklistCriterionRequest> Criteria, int RequiredPassCount);

/// <summary>Body for editing a Draft checklist configuration's criteria + required-pass count.</summary>
public record UpdateChecklistConfigRequest(
    IReadOnlyList<ChecklistCriterionRequest> Criteria, int RequiredPassCount);

/// <summary>Body for copying a checklist configuration into another semester.</summary>
public record CopyChecklistConfigRequest(int TargetSemesterId);
