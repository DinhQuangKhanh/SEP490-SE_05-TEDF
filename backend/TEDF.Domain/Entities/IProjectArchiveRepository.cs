namespace TEDF.Domain.Entities
{
    /// <summary>Repository contract for archived (completed) projects.</summary>
    public interface IProjectArchiveRepository
    {
        Task<IReadOnlyList<ProjectArchive>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ProjectArchive?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        void Update(ProjectArchive archive);
    }
}
