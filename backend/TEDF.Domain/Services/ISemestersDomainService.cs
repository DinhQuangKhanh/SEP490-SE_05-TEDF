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

    Task<EligibleMentorsImportResult> ImportEligibleMentorsAsync(
        int semesterId, Stream fileStream, string fileName, Guid importedBy, CancellationToken cancellationToken = default);

    /// <summary>Finalizes the roster and dispatches mentor notifications + the student-email job.</summary>
    Task PublishRosterAsync(int semesterId, Guid publishedBy, CancellationToken cancellationToken = default);

    /// <summary>Corrects the assigned program of a rostered mentor (inline admin edit).</summary>
    Task UpdateEligibleMentorMajorAsync(int semesterId, Guid mentorId, int majorId, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes eligible students from the roster.</summary>
    Task RemoveEligibleStudentsAsync(int semesterId, IReadOnlyList<Guid> studentIds, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes eligible mentors from the roster.</summary>
    Task RemoveEligibleMentorsAsync(int semesterId, IReadOnlyList<Guid> mentorIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a mentor may supervise/evaluate in the given semester: assigned on the eligible-mentor roster,
    /// OR already owning a pool topic / active supervision there. Used to gate supervising actions.
    /// </summary>
    Task<bool> IsMentorAssignedAsync(Guid mentorId, int semesterId, CancellationToken cancellationToken = default);
}

/// <summary>A phase to add when creating a semester.</summary>
public record NewSemesterPhase(string Name, string Type, DateTime StartDate, DateTime EndDate);

/// <summary>A phase date change when updating a semester.</summary>
public record SemesterPhaseDateChange(int Id, DateTime StartDate, DateTime EndDate);

/// <summary>A row that was not imported, with the reason (để cảnh báo người dùng).</summary>
public record ImportRowIssue(string Code, string Reason);

/// <summary>Result of importing eligible students from a spreadsheet.</summary>
public record EligibleStudentsImportResult(int TotalProcessed, int SuccessfullyImported, IReadOnlyList<ImportRowIssue> Issues);

/// <summary>Result of importing eligible (supervising) mentors from a spreadsheet.</summary>
public record EligibleMentorsImportResult(int TotalProcessed, int SuccessfullyImported, IReadOnlyList<ImportRowIssue> Issues);
