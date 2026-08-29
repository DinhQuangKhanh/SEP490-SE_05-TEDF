using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;

namespace TEDF.Domain.Services
{
    /// <summary>
    /// Write-side helper for the Projects feature. Currently the single source of truth for the
    /// project-code format; reads go through <c>IProjectsQueryService</c>.
    /// </summary>
    public interface IProjectsDomainService
    {
        /// <summary>
        /// Generates the next project code for a semester and major, e.g. "FA26-SE-01".
        /// Single source of truth for the code format.
        /// </summary>
        Task<ProjectCode> GenerateProjectCodeAsync(int semesterId, int majorId, CancellationToken cancellationToken = default);
    }
}
