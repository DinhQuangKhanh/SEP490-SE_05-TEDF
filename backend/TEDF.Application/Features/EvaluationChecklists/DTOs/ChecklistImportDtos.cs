namespace TEDF.Application.Features.EvaluationChecklists.DTOs;

// ── Parser output (internal, produced by IChecklistExcelService) ─────────────

/// <summary>
/// One parsed row from an imported checklist Excel file, with any per-row validation errors.
/// <see cref="MaxScore"/>/<see cref="PassScore"/> are null when the cell was blank or non-numeric
/// (the corresponding error is then present in <see cref="Errors"/>).
/// </summary>
public record ChecklistImportRow(
    int RowNumber,
    string TitleVi,
    string TitleEn,
    string? Description,
    decimal? MaxScore,
    decimal? PassScore,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Full result of parsing a checklist Excel file: the ordered rows plus file-level errors.</summary>
public record ChecklistImportParseResult(
    IReadOnlyList<ChecklistImportRow> Rows,
    IReadOnlyList<string> GlobalErrors)
{
    /// <summary>True when the file has at least one row and no row-level or file-level errors.</summary>
    public bool IsValid => GlobalErrors.Count == 0 && Rows.Count > 0 && Rows.All(r => r.IsValid);
}

// ── Preview DTO (returned to the Department Head before confirming import) ────

/// <summary>One criterion row shown in the import preview, echoing its 1-based order and any errors.</summary>
public record ChecklistImportPreviewRowDto(
    int RowNumber,
    int Order,
    string TitleVi,
    string TitleEn,
    string? Description,
    decimal? MaxScore,
    decimal? PassScore,
    IReadOnlyList<string> Errors);

/// <summary>
/// Preview of a parsed checklist file: the rows, whether it is safe to import, aggregated errors and the
/// count of valid criteria. Data errors are reported here (HTTP 200) — never as a 500.
/// </summary>
public record ChecklistImportPreviewDto(
    bool IsValid,
    int CriteriaCount,
    IReadOnlyList<ChecklistImportPreviewRowDto> Rows,
    IReadOnlyList<string> Errors);
