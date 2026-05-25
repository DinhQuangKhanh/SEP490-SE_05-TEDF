
namespace TEDF.Domain.Services
{
    public interface ISemesterDomainService
    {
        /// <summary>
        /// Gets the currently active semester.
        /// </summary>
        Task<int?> GetActiveSemesterIdAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current phase of a semester.
        /// </summary>
        Task<int?> GetCurrentPhaseIdAsync(int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a date is within a specific phase.
        /// </summary>
        Task<bool> IsWithinPhaseAsync(int semesterId, int phaseId, DateTime date, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the semester that is N semesters after the given semester.
        /// Returns null when the target semester does not exist yet.
        /// </summary>
        Task<int?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default);
    }
}
