namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the Semesters feature (also exposes phase/active-semester helpers
/// used by other features' write flows). Command handlers depend on this only.
/// </summary>
public interface ISemestersDomainService
{
    // --- Helper queries used by other features' write flows ---
    Task<int?> GetActiveSemesterIdAsync(CancellationToken cancellationToken = default);
    Task<int?> GetCurrentPhaseIdAsync(int semesterId, CancellationToken cancellationToken = default);
    Task<bool> IsWithinPhaseAsync(int semesterId, int phaseId, DateTime date, CancellationToken cancellationToken = default);
    Task<int?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default);

    // --- Semesters feature write operations ---
    Task<int> CreateAsync(
        string name, string code, DateTime startDate, DateTime endDate,
        int academicYearStart, string? description,
        IReadOnlyList<NewSemesterPhase> phases, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id, string name, string? description, DateTime startDate, DateTime endDate,
        IReadOnlyList<SemesterPhaseDateChange>? phases, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<EligibleStudentsImportResult> ImportEligibleStudentsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken cancellationToken = default);
}

/// <summary>A phase to add when creating a semester.</summary>
public record NewSemesterPhase(string Name, string Type, DateTime StartDate, DateTime EndDate);

/// <summary>A phase date change when updating a semester.</summary>
public record SemesterPhaseDateChange(int Id, DateTime StartDate, DateTime EndDate);

/// <summary>Result of importing eligible students from a spreadsheet.</summary>
public record EligibleStudentsImportResult(int TotalProcessed, int SuccessfullyImported, IReadOnlyList<string> FailedStudentCodes);
