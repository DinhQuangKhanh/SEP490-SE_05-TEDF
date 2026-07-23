using System.Globalization;
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
        var maxRaw = GetString(row, columns, ColumnKey.MaxScore);
        var passRaw = GetString(row, columns, ColumnKey.PassScore);

        if (IsBlankRow(titleVi, titleEn, description, maxRaw, passRaw))
            return null;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(titleVi))
            errors.Add("Tên tiêu chí (tiếng Việt) không được để trống.");

        var maxScore = TryParseScore(maxRaw, out var maxOk);
        var passScore = TryParseScore(passRaw, out var passOk);
        ValidateScores(maxScore, maxOk, passScore, passOk, errors);

        return new ChecklistImportRow(
            RowNumber: rowNumber,
            TitleVi: titleVi.Trim(),
            TitleEn: titleEn.Trim(),
            Description: string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            MaxScore: maxOk ? maxScore : null,
            PassScore: passOk ? passScore : null,
            Errors: errors);
    }

    private static bool IsBlankRow(params string[] values) => values.All(string.IsNullOrWhiteSpace);

    private static void ValidateScores(decimal maxScore, bool maxOk, decimal passScore, bool passOk, List<string> errors)
    {
        if (!maxOk)
            errors.Add("Điểm tối đa không hợp lệ (phải là số).");
        else if (maxScore <= 0)
            errors.Add("Điểm tối đa phải lớn hơn 0.");

        if (!passOk)
            errors.Add("Điểm đạt không hợp lệ (phải là số).");
        else if (passScore < 0)
            errors.Add("Điểm đạt không được âm.");

        if (maxOk && passOk && maxScore > 0 && passScore > maxScore)
            errors.Add("Điểm đạt không được lớn hơn điểm tối đa.");
    }

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Checklist");

        string[] headers =
        [
            "STT", "Tên tiêu chí (tiếng Việt)", "Tên tiêu chí (tiếng Anh)",
            "Mô tả / Nội dung", "Điểm tối đa", "Điểm đạt"
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
            ws.Cell(rowNumber, 5).Value = DefaultChecklistCriteria.DefaultMaxScore;
            ws.Cell(rowNumber, 6).Value = DefaultChecklistCriteria.DefaultPassScore;
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
    private enum ColumnKey { Order, TitleVi, TitleEn, Description, MaxScore, PassScore }

    private static readonly (ColumnKey Key, string Header)[] RequiredColumns =
    [
        (ColumnKey.TitleVi, "Tên tiêu chí (tiếng Việt)"),
        (ColumnKey.MaxScore, "Điểm tối đa"),
        (ColumnKey.PassScore, "Điểm đạt"),
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

            // Order matters: check the most specific keywords first.
            if (!map.ContainsKey(ColumnKey.MaxScore) && text.Contains("tối đa"))
                map[ColumnKey.MaxScore] = c;
            else if (!map.ContainsKey(ColumnKey.PassScore) && (text.Contains("điểm đạt") || (text.Contains("đạt") && text.Contains("điểm"))))
                map[ColumnKey.PassScore] = c;
            else if (!map.ContainsKey(ColumnKey.TitleEn) && text.Contains("anh"))
                map[ColumnKey.TitleEn] = c;
            else if (!map.ContainsKey(ColumnKey.TitleVi) && (text.Contains("việt") || text.Contains("tên tiêu chí")))
                map[ColumnKey.TitleVi] = c;
            else if (!map.ContainsKey(ColumnKey.Description) && (text.Contains("mô tả") || text.Contains("nội dung")))
                map[ColumnKey.Description] = c;
            else if (!map.ContainsKey(ColumnKey.Order) && (text.Contains("stt") || text.Contains("thứ tự")))
                map[ColumnKey.Order] = c;
        }

        return map;
    }

    private static string GetString(IXLRow row, IReadOnlyDictionary<ColumnKey, int> columns, ColumnKey key)
        => columns.TryGetValue(key, out var col) ? row.Cell(col).GetString() : string.Empty;

    private static decimal TryParseScore(string raw, out bool ok)
    {
        raw = (raw ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            ok = false;
            return 0m;
        }

        // Accept both "7.5" and Vietnamese "7,5".
        var normalized = raw.Replace(",", ".");
        ok = decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value);
        return ok ? value : 0m;
    }
}
