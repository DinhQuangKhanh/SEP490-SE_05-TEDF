using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Parses and generates the Department-Head checklist Excel files. Implemented in the Infrastructure layer
/// (ClosedXML). Parsing is tolerant: structural/format/value problems are reported as row-level errors
/// rather than thrown, so the caller can surface them per row instead of returning a 500.
/// </summary>
public interface IChecklistExcelService
{
    /// <summary>Parses a checklist workbook into ordered rows plus file-level errors.</summary>
    ChecklistImportParseResult Parse(byte[] fileContent);

    /// <summary>Builds the blank checklist import template (with header row + one example row).</summary>
    byte[] GenerateTemplate();
}
