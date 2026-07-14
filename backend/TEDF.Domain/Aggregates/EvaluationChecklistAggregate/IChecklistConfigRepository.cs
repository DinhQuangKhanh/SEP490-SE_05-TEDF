using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.EvaluationChecklistAggregate;

/// <summary>Repository for the <see cref="ChecklistConfig"/> aggregate.</summary>
public interface IChecklistConfigRepository : IRepository<ChecklistConfig, Guid>
{
    /// <summary>The single Active config for a semester, or null if none is configured.</summary>
    Task<ChecklistConfig?> GetActiveBySemesterAsync(int semesterId, CancellationToken cancellationToken = default);

    /// <summary>All configs for a semester (any status), newest version first.</summary>
    Task<IReadOnlyList<ChecklistConfig>> GetBySemesterAsync(int semesterId, CancellationToken cancellationToken = default);

    /// <summary>All configs across all semesters (for the management list).</summary>
    Task<IReadOnlyList<ChecklistConfig>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>True if an Active config already exists for the semester (optionally excluding one id).</summary>
    Task<bool> ExistsActiveForSemesterAsync(int semesterId, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Highest version number used for the semester (0 when none).</summary>
    Task<int> GetMaxVersionForSemesterAsync(int semesterId, CancellationToken cancellationToken = default);

    /// <summary>True when at least one saved evaluation result references this config version.</summary>
    Task<bool> HasResultsAsync(Guid checklistConfigId, CancellationToken cancellationToken = default);
}
