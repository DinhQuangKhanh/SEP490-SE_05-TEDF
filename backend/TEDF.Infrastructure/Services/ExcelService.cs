using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Services;

public class ExcelService : IExcelService
{
    // Header aliases (normalized: lowercased, diacritics & non-alphanumerics stripped).
    private static readonly string[] StudentCodeKeys = ["mssv", "studentcode", "studentid", "masinhvien", "masv"];
    private static readonly string[] MentorCodeKeys = ["msgv", "magiangvien", "magv", "employeecode", "staffcode", "manhanvien", "manv"];
    private static readonly string[] EmailKeys = ["email", "emailfpt", "thudientu"];
    private static readonly string[] PhoneKeys = ["phone", "phonenumber", "sodienthoai", "dienthoai", "sdt"];
    private static readonly string[] ProgramKeys = ["program", "programcode", "major", "majorcode", "nganh", "chuyennganh"];
    private static readonly string[] DivisionKeys = ["bomon", "bomongiangday", "division"];
    private static readonly string[] FullNameKeys = ["fullname", "name", "hoten", "hovaten", "tengiangvien"];

    public Task<List<string>> ExtractStudentCodesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var codes = ReadRows(fileStream, fileName)
            .Select(r => FirstNonEmpty(r, StudentCodeKeys))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.ToUpperInvariant())
            .Distinct()
            .ToList();
        return Task.FromResult(codes);
    }

    public Task<List<EligibleStudentRow>> ExtractEligibleStudentRowsAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var rows = ReadRows(fileStream, fileName)
            .Select(r => new
            {
                Code = FirstNonEmpty(r, StudentCodeKeys),
                Name = FirstNonEmpty(r, FullNameKeys),
                Email = FirstNonEmpty(r, EmailKeys),
                Phone = FirstNonEmpty(r, PhoneKeys),
                Program = FirstNonEmpty(r, ProgramKeys)
            })
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .GroupBy(r => r.Code!.ToUpperInvariant())
            .Select(g => g.First())
            .Select(r => new EligibleStudentRow(r.Code!.ToUpperInvariant(), r.Name, r.Email, r.Phone, r.Program))
            .ToList();
        return Task.FromResult(rows);
    }

    public Task<List<EligibleMentorRow>> ExtractEligibleMentorRowsAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var rows = ReadRows(fileStream, fileName)
            .Select(r => new
            {
                Code = FirstNonEmpty(r, MentorCodeKeys),
                Name = FirstNonEmpty(r, FullNameKeys),
                Email = FirstNonEmpty(r, EmailKeys),
                Phone = FirstNonEmpty(r, PhoneKeys),
                Program = FirstNonEmpty(r, ProgramKeys),
                Division = FirstNonEmpty(r, DivisionKeys)
            })
            .Where(r => !string.IsNullOrWhiteSpace(r.Code))
            .GroupBy(r => r.Code!.ToUpperInvariant())
            .Select(g => g.First())
            .Select(r => new EligibleMentorRow(r.Code!.ToUpperInvariant(), r.Name, r.Email, r.Phone, r.Program, r.Division))
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Dispatches to the correct parser based on file extension.</summary>
    private static List<Dictionary<string, string>> ReadRows(Stream fileStream, string fileName)
    {
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return ReadCsvRows(fileStream);

        if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return ReadXlsxRows(fileStream);

        return [];
    }

    private static List<Dictionary<string, string>> ReadCsvRows(Stream fileStream)
    {
        var records = new List<Dictionary<string, string>>();
        using var reader = new StreamReader(fileStream);
        string[]? headers = null;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (headers is null)
            {
                headers = line.Split(',').Select(NormalizeHeader).ToArray();
                continue;
            }

            var cols = line.Split(',');
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < headers.Length && i < cols.Length; i++)
            {
                if (!string.IsNullOrEmpty(headers[i]) && !dict.ContainsKey(headers[i]))
                    dict[headers[i]] = cols[i].Trim();
            }
            records.Add(dict);
        }

        return records;
    }

    private static List<Dictionary<string, string>> ReadXlsxRows(Stream fileStream)
    {
        var records = new List<Dictionary<string, string>>();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        var usedRows = worksheet?.RangeUsed()?.RowsUsed()?.ToList();
        if (usedRows is null || usedRows.Count == 0) return records;

        var headerMap = new Dictionary<int, string>(); // column number -> normalized header
        foreach (var cell in usedRows[0].CellsUsed())
        {
            var header = NormalizeHeader(cell.GetValue<string>());
            if (!string.IsNullOrEmpty(header) && !headerMap.ContainsValue(header))
                headerMap[cell.Address.ColumnNumber] = header;
        }

        foreach (var row in usedRows.Skip(1))
        {
            var dict = new Dictionary<string, string>();
            foreach (var (column, header) in headerMap)
            {
                var value = row.Cell(column).GetValue<string>()?.Trim();
                if (!string.IsNullOrEmpty(value))
                    dict[header] = value;
            }
            records.Add(dict);
        }

        return records;
    }

    private static string? FirstNonEmpty(Dictionary<string, string> record, string[] keys)
    {
        foreach (var key in keys)
            if (record.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        return null;
    }

    /// <summary>Lowercases, strips Vietnamese diacritics and non-alphanumerics so headers match aliases.</summary>
    private static string NormalizeHeader(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var decomposed = raw.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString().Replace("đ", "d");
    }
}
