using Microsoft.Extensions.Logging;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Infrastructure.Caching
{
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task InvalidateProjectCacheAsync(Guid projectId, CancellationToken ct = default)
        {
            await _cacheService.RemoveAsync(CacheKeys.ProjectById(projectId), ct);
            await _cacheService.RemoveByPrefixAsync("stats:project:", ct);
            _logger.LogInformation("Project cache invalidated: {ProjectId}", projectId);
        }

        public async Task InvalidateGroupCacheAsync(Guid groupId, CancellationToken ct = default)
        {
            await _cacheService.RemoveAsync(CacheKeys.GroupById(groupId), ct);
            _logger.LogInformation("Group cache invalidated: {GroupId}", groupId);
        }

        public async Task InvalidateSemesterCacheAsync(int semesterId, CancellationToken ct = default)
        {
            await _cacheService.RemoveAsync(CacheKeys.SemesterById(semesterId), ct);
            await _cacheService.RemoveAsync(CacheKeys.ActiveSemester, ct);
            await _cacheService.RemoveAsync(CacheKeys.ProjectStats(semesterId), ct);
            await _cacheService.RemoveAsync(CacheKeys.EvaluationStats(semesterId), ct);
            _logger.LogInformation("Semester cache invalidated: {SemesterId}", semesterId);
        }

        public async Task InvalidateUserPermissionsCacheAsync(Guid userId, CancellationToken ct = default)
        {
            await _cacheService.RemoveAsync(CacheKeys.UserPermissions(userId), ct);
            _logger.LogInformation("User permissions cache invalidated: {UserId}", userId);
        }

        public async Task InvalidateEvaluatorCacheAsync(Guid evaluatorId, CancellationToken ct = default)
        {
            await _cacheService.RemoveByPrefixAsync($"evaluator:{evaluatorId}:", ct);
            _logger.LogInformation("Evaluator cache invalidated: {EvaluatorId}", evaluatorId);
        }

        public async Task InvalidateEvaluatorFilterOptionsCacheAsync(CancellationToken ct = default)
        {
            await _cacheService.RemoveAsync(CacheKeys.EvaluatorFilterOptions, ct);
            _logger.LogInformation("Evaluator filter options cache invalidated");
        }

        public async Task InvalidateUserListCacheAsync(CancellationToken ct = default)
        {
            await _cacheService.RemoveByPrefixAsync(CacheKeys.UserListPrefix, ct);
            _logger.LogInformation("User list cache invalidated");
        }

        public async Task InvalidateAllAsync(CancellationToken ct = default)
        {
            await _cacheService.RemoveByPrefixAsync("", ct);
            _logger.LogWarning("All cache invalidated");
        }
    }
}
