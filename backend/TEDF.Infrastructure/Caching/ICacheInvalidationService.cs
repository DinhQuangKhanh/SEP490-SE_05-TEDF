namespace TEDF.Infrastructure.Caching
{
    public interface ICacheInvalidationService
    {
        Task InvalidateProjectCacheAsync(Guid projectId, CancellationToken ct = default);
        Task InvalidateGroupCacheAsync(Guid groupId, CancellationToken ct = default);
        Task InvalidateSemesterCacheAsync(int semesterId, CancellationToken ct = default);
        Task InvalidateUserPermissionsCacheAsync(Guid userId, CancellationToken ct = default);
        Task InvalidateEvaluatorCacheAsync(Guid evaluatorId, CancellationToken ct = default);
        Task InvalidateEvaluatorFilterOptionsCacheAsync(CancellationToken ct = default);
        Task InvalidateUserListCacheAsync(CancellationToken ct = default);
        Task InvalidateAllAsync(CancellationToken ct = default);
    }
}
