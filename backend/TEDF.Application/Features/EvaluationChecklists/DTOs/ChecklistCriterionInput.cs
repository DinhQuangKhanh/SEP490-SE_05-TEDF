namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

/// <summary>
/// Editable criterion payload used by create/update commands. Order is derived from list position by the
/// domain, so it is not accepted from the client here. Scoring has been replaced by Pass/Fail evaluation.
/// </summary>
public record ChecklistCriterionInput(
    string TitleVi,
    string TitleEn,
    string? Description);
