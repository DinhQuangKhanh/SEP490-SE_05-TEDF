using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Features.EvaluationChecklists.Queries.PreviewChecklistImport;

public class PreviewChecklistImportQueryHandler
    : IQueryHandler<PreviewChecklistImportQuery, ChecklistImportPreviewDto>
{
    private readonly IChecklistExcelService _excel;

    public PreviewChecklistImportQueryHandler(IChecklistExcelService excel)
    {
        _excel = excel;
    }

    public Task<ChecklistImportPreviewDto> Handle(
        PreviewChecklistImportQuery request, CancellationToken cancellationToken)
    {
        var result = _excel.Parse(request.FileContent);

        var rows = result.Rows
            .Select((r, index) => new ChecklistImportPreviewRowDto(
                r.RowNumber, index + 1, r.TitleVi, r.TitleEn, r.Description, r.Errors))
            .ToList();

        var errors = result.GlobalErrors
            .Concat(result.Rows
                .Where(r => !r.IsValid)
                .SelectMany(r => r.Errors.Select(e => $"Dòng {r.RowNumber}: {e}")))
            .ToList();

        var dto = new ChecklistImportPreviewDto(
            IsValid: result.IsValid,
            CriteriaCount: result.Rows.Count,
            Rows: rows,
            Errors: errors);

        return Task.FromResult(dto);
    }
}
