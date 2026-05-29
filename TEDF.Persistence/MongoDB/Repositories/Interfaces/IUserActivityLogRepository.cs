using TEDF.Persistence.MongoDB.Documents;

namespace TEDF.Persistence.MongoDB.Repositories.Interfaces
{
    public interface IUserActivityLogRepository
    {
        Task AddAsync(UserActivityLogDocument log, CancellationToken ct = default);
        Task<IEnumerable<UserActivityLogDocument>> GetByUserIdAsync(Guid userId, int limit = 100, CancellationToken ct = default);

        /// <summary>
        /// Returns a paginated, filterable list of activity logs (flat, one row per log).
        /// </summary>
        Task<(IEnumerable<UserActivityLogDocument> Items, long TotalCount)> GetPagedAsync(
            string? role = null,
            string? category = null,
            string? severity = null,
            string? searchTerm = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        /// <summary>
        /// Returns activity logs grouped by (UserId, UserRole, Action, Category)
        /// with severity breakdown and per-role counts for stat cards.
        /// </summary>
        Task<(IEnumerable<GroupedActivityLogResult> Items, long TotalGroups, Dictionary<string, long> RoleCounts)>
            GetGroupedAsync(
                string? role = null,
                string? severity = null,
                string? searchTerm = null,
                DateTime? from = null,
                DateTime? to = null,
                int page = 1,
                int pageSize = 20,
                CancellationToken ct = default);

        /// <summary>
        /// Returns unique error messages with their occurrence count for a specific (userId, action) pair.
        /// </summary>
        Task<IEnumerable<ErrorDetailResult>> GetErrorDetailsAsync(
            Guid userId,
            string action,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default);

        /// <summary>
        /// Returns aggregate counts per severity level (info, warning, error, critical).
        /// </summary>
        Task<SeverityCountsResult> GetSeveritySummaryAsync(
            string? role = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default);
    }
}
