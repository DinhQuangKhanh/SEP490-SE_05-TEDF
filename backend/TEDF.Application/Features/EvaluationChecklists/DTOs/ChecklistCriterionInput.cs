namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

/// <summary>
/// Editable criterion payload used by create/update commands. Order is derived from list position by the
/// domain, so it is not accepted from the client here. Score bounds are validated by the domain.
/// </summary>
public record ChecklistCriterionInput(
    string TitleVi,
    string TitleEn,
    string? Description,
    decimal MaxScore,
    decimal PassScore);
