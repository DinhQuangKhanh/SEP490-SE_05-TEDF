using TEDF.Application.Features.EvaluationChecklists.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>Read-side service for the topic-evaluation checklist feature.</summary>
public interface IChecklistQueryService
{
    /// <summary>
    /// The evaluator's checklist for a project (applicable criteria + saved results). Returns null when the
    /// evaluator is not actively assigned to the project (or the project does not exist).
    /// </summary>
    Task<ProjectChecklistDto?> GetProjectChecklistAsync(
        Guid projectId, Guid evaluatorId, CancellationToken cancellationToken = default);

    /// <summary>Checklist configurations (optionally filtered by semester) plus the semester options.</summary>
    Task<ChecklistConfigListDto> GetConfigsAsync(int? semesterId, CancellationToken cancellationToken = default);

    /// <summary>A single checklist configuration with its criteria, or null when not found.</summary>
    Task<ChecklistConfigDto?> GetConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
