namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

/// <summary>
/// Editable criterion payload used by create/update/copy commands. Order is derived from list position
/// by the domain, so it is not accepted from the client here.
/// </summary>
public record ChecklistCriterionInput(
    string TitleVi,
    string TitleEn,
    string? Description);
