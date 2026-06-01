namespace TEDF.Application.Common.Interfaces;

public interface IExcelService
{
    /// <summary>
    /// Reads a stream of an Excel/CSV file and returns a list of student codes.
    /// Expects the student codes to be in the first column or under a header named "MSSV" or "StudentCode".
    /// </summary>
    Task<List<string>> ExtractStudentCodesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
