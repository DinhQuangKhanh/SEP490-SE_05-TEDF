namespace TEDF.Domain.Services
{
    /// <summary>
    /// Write-side service for the topic-evaluation checklist feature.
    /// Command handlers depend on this only (it resolves the current user and owns all business rules,
    /// authorization, repository access and unit-of-work).
    /// </summary>
    public interface IChecklistDomainService
    {
        // ── Evaluator ──

        /// <summary>
        /// Upserts the current evaluator's checklist result for a project. The domain validates each score
        /// against its snapshot bounds and recomputes the passed count / pass flags.
        /// </summary>
        Task SaveProjectChecklistAsync(
            Guid projectId,
            IReadOnlyList<ChecklistScoreData> scores,
            string? note,
            CancellationToken cancellationToken = default);

        // ── Department head: checklist configuration ──

        /// <summary>Creates a new Draft checklist configuration for a semester; returns its id.</summary>
        Task<Guid> CreateConfigAsync(
            int semesterId,
            IReadOnlyList<ChecklistCriterionData> criteria,
            int requiredPassCount,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new Draft checklist configuration by importing an Excel file. Parses + validates the
        /// file; on invalid data throws a validation error listing the offending rows (never a 500).
        /// </summary>
        Task<Guid> ImportConfigAsync(
            int semesterId,
            byte[] fileContent,
            string fileName,
            int requiredPassCount,
            CancellationToken cancellationToken = default);

        /// <summary>Clones a configuration into a new Draft for the target semester; returns the new id.</summary>
        Task<Guid> CopyConfigAsync(
            Guid sourceConfigId,
            int targetSemesterId,
            CancellationToken cancellationToken = default);

        /// <summary>Replaces a Draft configuration's criteria + required-pass count.</summary>
        Task UpdateConfigAsync(
            Guid id,
            IReadOnlyList<ChecklistCriterionData> criteria,
            int requiredPassCount,
            CancellationToken cancellationToken = default);

        /// <summary>Activates a configuration (retiring the previous Active one for its semester).</summary>
        Task ActivateConfigAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Retires a configuration (kept for history).</summary>
        Task DeactivateConfigAsync(Guid id, CancellationToken cancellationToken = default);
    }

    /// <summary>Editable criterion payload passed to the checklist domain service (layer-neutral).</summary>
    public record ChecklistCriterionData(
        string TitleVi, string TitleEn, string? Description, decimal MaxScore, decimal PassScore);

    /// <summary>One evaluator score entry passed to the checklist domain service (layer-neutral).</summary>
    public record ChecklistScoreData(Guid CriterionId, decimal? Score, string? Comment);
}
