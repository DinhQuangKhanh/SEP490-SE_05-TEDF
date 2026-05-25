namespace TEDF.Domain.Entities;

/// <summary>
/// Read-only repository interface for Major entity lookups.
/// </summary>
public interface IMajorReadRepository
{
    Task<Major?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
