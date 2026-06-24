namespace TEDF.Application.Common.Interfaces;

public interface IExcelService
{
    /// <summary>
    /// Reads a stream of an Excel/CSV file and returns a list of student codes.
    /// Expects the student codes to be in the first column or under a header named "MSSV" or "StudentCode".
    /// </summary>
    Task<List<string>> ExtractStudentCodesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an eligible-student spreadsheet, returning one row per student with the snapshot columns
    /// (code + optional Email / Phone / Program). Columns are detected by header name.
    /// </summary>
    Task<List<EligibleStudentRow>> ExtractEligibleStudentRowsAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an eligible-mentor spreadsheet, returning one row per mentor with the snapshot columns
    /// (employee code + optional Name / Email / Phone / Program). Columns are detected by header name.
    /// </summary>
    Task<List<EligibleMentorRow>> ExtractEligibleMentorRowsAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

/// <summary>A parsed eligible-student row from an imported spreadsheet.</summary>
public record EligibleStudentRow(string StudentCode, string? FullName, string? Email, string? PhoneNumber, string? ProgramCode);

/// <summary>A parsed eligible-mentor row from an imported spreadsheet.</summary>
public record EligibleMentorRow(string EmployeeCode, string? FullName, string? Email, string? PhoneNumber, string? ProgramCode, string? Division);
