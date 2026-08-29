using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Pool maintenance driven by the recurring Hangfire job: expire stale topics and backfill missing
/// expiration metadata. Extracted from the former god-service (single responsibility).
/// </summary>
public sealed class PoolLifecycleService : IPoolLifecycleService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITopicPoolRepository _topicPoolRepository;
    private readonly ISemestersDomainService _semesterDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PoolLifecycleService> _logger;

    public PoolLifecycleService(
        IProjectRepository projectRepository,
        ITopicPoolRepository topicPoolRepository,
        ISemestersDomainService semesterDomainService,
        IUnitOfWork unitOfWork,
        ILogger<PoolLifecycleService> logger)
    {
        _projectRepository = projectRepository;
        _topicPoolRepository = topicPoolRepository;
        _semesterDomainService = semesterDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ExpireOldTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default)
    {
        // Find all projects that should be expired
        var expiredProjects = await _projectRepository.GetExpirablePoolTopicsAsync(currentSemesterId, cancellationToken);

        foreach (var project in expiredProjects)
        {
            project.MarkAsExpired();
            _projectRepository.Update(project);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} topics in semester {SemesterId}", expiredProjects.Count, currentSemesterId);

        return expiredProjects.Count;
    }

    public async Task<int> ResolveMissingExpirationSemestersAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _projectRepository.GetPoolTopicsMissingExpirationAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return 0;
        }

        var resolvedCount = 0;
        foreach (var project in candidates)
        {
            if (!project.TopicPoolId.HasValue)
            {
                continue;
            }

            var pool = await _topicPoolRepository.GetByIdAsync(project.TopicPoolId.Value, cancellationToken);
            if (pool is null)
            {
                continue;
            }

            // ExpirationSemesters counts registration rounds from the semester the topic runs in, so
            // the hop count is offset-1 from SemesterId. Rows written before SemesterId and
            // CreatedInSemesterId were told apart carry the *proposal* semester in both columns —
            // for those, keep the original offset so their expiry does not move a semester earlier.
            var expirationOffset = Math.Max(1, pool.ExpirationSemesters);
            var isLegacyStamp = project.CreatedInSemesterId == project.SemesterId;
            var expirationSemesterId = await _semesterDomainService.GetSemesterAfterAsync(
                project.SemesterId,
                isLegacyStamp ? expirationOffset : expirationOffset - 1,
                cancellationToken);

            if (!expirationSemesterId.HasValue)
            {
                continue;
            }

            project.SetExpirationSemester(expirationSemesterId.Value);
            _projectRepository.Update(project);
            resolvedCount++;
        }

        if (resolvedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Resolved expiration semester for {Count} pool topics.", resolvedCount);
        return resolvedCount;
    }
}
