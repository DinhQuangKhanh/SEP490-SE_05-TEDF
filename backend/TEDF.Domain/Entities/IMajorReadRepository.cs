namespace TEDF.Domain.Entities;

/// <summary>
/// Read-only repository interface for Major entity lookups.
/// </summary>
public interface IMajorReadRepository
{
    Task<Major?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Resolves a Major by its code (e.g. "BIT_SE_18C_NodeJS"); case-insensitive.</summary>
    Task<Major?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IEnumerable<Major>> GetAllAsync(CancellationToken cancellationToken = default);
}
