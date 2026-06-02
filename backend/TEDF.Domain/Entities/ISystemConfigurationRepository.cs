namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Repository contract for the key/value <see cref="SystemConfiguration"/> store.
    /// </summary>
    public interface ISystemConfigurationRepository
    {
        Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsKeyAsync(string key, CancellationToken cancellationToken = default);
        Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
        Task AddAsync(SystemConfiguration config, CancellationToken cancellationToken = default);
        void Update(SystemConfiguration config);
    }
}
