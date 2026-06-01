using ClosedXML.Excel;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Services;

public class ExcelService : IExcelService
{
    public Task<List<string>> ExtractStudentCodesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var studentCodes = new List<string>();

        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            using var reader = new StreamReader(fileStream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');
                var code = columns[0].Trim();
                // Simple validation to ignore headers
                if (code.Length > 2 && !code.Equals("mssv", StringComparison.OrdinalIgnoreCase))
                {
                    studentCodes.Add(code.ToUpperInvariant());
                }
            }
        }
        else if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet != null)
            {
                var rows = worksheet.RangeUsed()?.RowsUsed();
                if (rows == null || !rows.Any()) return Task.FromResult(studentCodes);
                
                // Try to find column index for "MSSV" or default to 1
                int codeColumnIndex = 1;
                var headerRow = rows.FirstOrDefault();
                if (headerRow != null)
                {
                    foreach (var cell in headerRow.CellsUsed())
                    {
                        if (cell.GetValue<string>().Equals("mssv", StringComparison.OrdinalIgnoreCase) || 
                            cell.GetValue<string>().Equals("studentcode", StringComparison.OrdinalIgnoreCase))
                        {
                            codeColumnIndex = cell.Address.ColumnNumber;
                            break;
                        }
                    }
                }

                // Skip header logic roughly applied
                bool isFirstRow = true;
                foreach (var row in rows)
                {
                    if (isFirstRow) 
                    {
                        isFirstRow = false;
                        continue;
                    }

                    var cellValue = row.Cell(codeColumnIndex).GetValue<string>()?.Trim();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        studentCodes.Add(cellValue.ToUpperInvariant());
                    }
                }
            }
        }

        return Task.FromResult(studentCodes.Distinct().ToList());
    }
}
