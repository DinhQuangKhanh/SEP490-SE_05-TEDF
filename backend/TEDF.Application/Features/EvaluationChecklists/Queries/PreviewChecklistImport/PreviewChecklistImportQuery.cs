using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.PreviewChecklistImport;

/// <summary>
/// Parses an uploaded checklist Excel file and returns a preview (rows + per-row errors) so the Department
/// Head can review the content before confirming the import. Not cached (input is the file bytes).
/// </summary>
public record PreviewChecklistImportQuery(byte[] FileContent, string FileName)
    : IQuery<ChecklistImportPreviewDto>;
