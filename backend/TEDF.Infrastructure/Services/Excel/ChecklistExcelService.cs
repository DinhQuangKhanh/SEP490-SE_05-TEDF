using ClosedXML.Excel;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

namespace TEDF.Infrastructure.Services.Excel;

/// <summary>
/// ClosedXML implementation of <see cref="IChecklistExcelService"/>. Columns are located by header keyword
/// (row 1) so column order is flexible. Parsing never throws for bad data — problems are returned as
/// row-level or file-level errors so the API surfaces a 400/preview instead of a 500.
/// </summary>
public sealed class ChecklistExcelService : IChecklistExcelService
{
    // Header row is row 1; data starts at row 2.
    private const int HeaderRow = 1;
    private const int FirstDataRow = 2;

    public ChecklistImportParseResult Parse(byte[] fileContent)
    {
        var globalErrors = new List<string>();

        if (fileContent is null || fileContent.Length == 0)
            return new ChecklistImportParseResult([], ["File import rỗng hoặc không hợp lệ."]);

        IXLWorksheet worksheet;
        XLWorkbook workbook;
        try
        {
            using var ms = new MemoryStream(fileContent);
            workbook = new XLWorkbook(ms);
            worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException("empty workbook");
        }
        catch
        {
            return new ChecklistImportParseResult([], ["File không đúng định dạng Excel (.xlsx)."]);
        }

        using (workbook)
        {
            var columns = MapColumns(worksheet);

            foreach (var (key, header) in RequiredColumns)
            {
                if (!columns.ContainsKey(key))
                    globalErrors.Add($"Thiếu cột bắt buộc: \"{header}\".");
            }

            if (globalErrors.Count > 0)
                return new ChecklistImportParseResult([], globalErrors);

            var rows = ReadRows(worksheet, columns);

            if (rows.Count == 0)
                globalErrors.Add("File không có tiêu chí nào. Vui lòng thêm ít nhất một dòng tiêu chí.");

            return new ChecklistImportParseResult(rows, globalErrors);
        }
    }

    private static List<ChecklistImportRow> ReadRows(IXLWorksheet worksheet, IReadOnlyDictionary<ColumnKey, int> columns)
    {
        var rows = new List<ChecklistImportRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (var rowNumber = FirstDataRow; rowNumber <= lastRow; rowNumber++)
        {
            var parsed = ParseDataRow(worksheet.Row(rowNumber), rowNumber, columns);
            if (parsed is not null)
                rows.Add(parsed);
        }

        return rows;
    }

    /// <summary>Parses one data row into a criterion, or null if the row is entirely blank.</summary>
    private static ChecklistImportRow? ParseDataRow(IXLRow row, int rowNumber, IReadOnlyDictionary<ColumnKey, int> columns)
    {
        var titleVi = GetString(row, columns, ColumnKey.TitleVi);
        var titleEn = GetString(row, columns, ColumnKey.TitleEn);
        var description = GetString(row, columns, ColumnKey.Description);

        if (IsBlankRow(titleVi, titleEn, description))
            return null;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(titleVi))
            errors.Add("Tên tiêu chí (tiếng Việt) không được để trống.");

        return new ChecklistImportRow(
            RowNumber: rowNumber,
            TitleVi: titleVi.Trim(),
            TitleEn: titleEn.Trim(),
            Description: string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Errors: errors);
    }

    private static bool IsBlankRow(params string[] values) => values.All(string.IsNullOrWhiteSpace);

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Checklist");

        string[] headers =
        [
            "STT", "Tên tiêu chí (tiếng Việt)", "Tên tiêu chí (tiếng Anh)",
            "Mô tả / Nội dung"
        ];

        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(HeaderRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var rowNumber = FirstDataRow;
        var order = 1;
        foreach (var item in DefaultChecklistCriteria.Items)
        {
            ws.Cell(rowNumber, 1).Value = order;
            ws.Cell(rowNumber, 2).Value = item.TitleVi;
            ws.Cell(rowNumber, 3).Value = item.TitleEn;
            ws.Cell(rowNumber, 4).Value = item.Description;
            rowNumber++;
            order++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 40;
        ws.Column(4).Width = 60;

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    // ── Column mapping ───────────────────────────────────────────────────────
    private enum ColumnKey { Order, TitleVi, TitleEn, Description }

    private static readonly (ColumnKey Key, string Header)[] RequiredColumns =
    [
        (ColumnKey.TitleVi, "Tên tiêu chí (tiếng Việt)")
    ];

    /// <summary>
    /// Header keyword rules, ordered from the most specific to the most generic — a header cell is
    /// claimed by the first rule that matches and whose column is not mapped yet.
    /// </summary>
    private static readonly (ColumnKey Key, Func<string, bool> Matches)[] HeaderRules =
    [
        (ColumnKey.TitleEn, t => t.Contains("anh")),
        (ColumnKey.TitleVi, t => t.Contains("việt") || t.Contains("tên tiêu chí")),
        (ColumnKey.Description, t => t.Contains("mô tả") || t.Contains("nội dung")),
        (ColumnKey.Order, t => t.Contains("stt") || t.Contains("thứ tự")),
    ];

    private static Dictionary<ColumnKey, int> MapColumns(IXLWorksheet worksheet)
    {
        var map = new Dictionary<ColumnKey, int>();
        var header = worksheet.Row(HeaderRow);
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (var c = 1; c <= lastCol; c++)
        {
            var text = header.Cell(c).GetString().Trim().ToLowerInvariant();
            if (text.Length == 0) continue;

            var key = ResolveColumnKey(text, map);
            if (key.HasValue) map[key.Value] = c;
        }

        return map;
    }

    /// <summary>Returns the column this header cell describes, or null if it matches nothing left.</summary>
    private static ColumnKey? ResolveColumnKey(string headerText, Dictionary<ColumnKey, int> mapped)
    {
        foreach (var (key, matches) in HeaderRules)
        {
            if (!mapped.ContainsKey(key) && matches(headerText))
                return key;
        }

        return null;
    }

    private static string GetString(IXLRow row, IReadOnlyDictionary<ColumnKey, int> columns, ColumnKey key)
        => columns.TryGetValue(key, out var col) ? row.Cell(col).GetString() : string.Empty;
}
