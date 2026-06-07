using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// EF Core implementation of topic pool container read queries.
/// Handles pool-level queries only (pool metadata, statistics, department grouping).
/// For individual topic queries, see <see cref="TopicQueryService"/>.
/// </summary>
public class TopicPoolsQueryService : ITopicPoolsQueryService
{
    private readonly AppDbContext _context;

    public TopicPoolsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TopicPoolDto>> GetTopicPoolsAsync(int? majorId, CancellationToken cancellationToken = default)
    {
        var query = _context.TopicPools.AsNoTracking();

        if (majorId.HasValue)
        {
            query = query.Where(tp => tp.MajorId == majorId.Value);
        }

        return await query
            .OrderBy(tp => tp.MajorId)
            .Select(tp => new TopicPoolDto
            {
                Id = tp.Id,
                Code = tp.Code,
                Name = tp.Name,
                Description = tp.Description,
                MajorId = tp.MajorId,
                Status = tp.Status,
                MaxActiveTopicsPerMentor = tp.MaxActiveTopicsPerMentor,
                ExpirationSemesters = tp.ExpirationSemesters,
                CreatedAt = tp.CreatedAt,
                UpdatedAt = tp.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DepartmentWithPoolsDto>> GetPoolsByDepartmentAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.Name,
                Majors = _context.Majors
                    .Where(m => m.DepartmentId == d.Id && m.IsActive)
                    .OrderBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Id,
                        m.Code,
                        m.Name,
                        Pool = _context.TopicPools
                            .Where(tp => tp.MajorId == m.Id)
                            .OrderByDescending(tp => tp.UpdatedAt ?? tp.CreatedAt)
                            .Select(tp => new
                            {
                                tp.Id,
                                tp.Code,
                                tp.Name,
                                tp.Status,
                                TotalTopics = _context.Projects.Count(p =>
                                    p.TopicPoolId == tp.Id &&
                                    p.SourceType == ProjectSourceType.FromPool)
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return departments.Select(d => new DepartmentWithPoolsDto
        {
            DepartmentId = d.Id,
            DepartmentCode = d.Code,
            DepartmentName = d.Name,
            Majors = d.Majors.Select(m => new MajorWithPoolDto
            {
                MajorId = m.Id,
                MajorCode = m.Code,
                MajorName = m.Name,
                Pool = m.Pool is null
                    ? null
                    : new TopicPoolSummaryDto
                    {
                        Id = m.Pool.Id,
                        Code = m.Pool.Code,
                        Name = m.Pool.Name,
                        StatusName = m.Pool.Status.ToString(),
                        TotalTopics = m.Pool.TotalTopics,
                    }
            }).ToList()
        }).ToList();
    }

    public async Task<TopicPoolDto?> GetTopicPoolByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TopicPools
            .AsNoTracking()
            .Where(tp => tp.Id == id)
            .Select(tp => new TopicPoolDto
            {
                Id = tp.Id,
                Code = tp.Code,
                Name = tp.Name,
                Description = tp.Description,
                MajorId = tp.MajorId,
                Status = tp.Status,
                MaxActiveTopicsPerMentor = tp.MaxActiveTopicsPerMentor,
                ExpirationSemesters = tp.ExpirationSemesters,
                CreatedAt = tp.CreatedAt,
                UpdatedAt = tp.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TopicPoolStatisticsDto> GetTopicPoolStatisticsAsync(Guid poolId, CancellationToken cancellationToken = default)
    {
        // Get pool info
        var pool = await _context.TopicPools
            .AsNoTracking()
            .Where(tp => tp.Id == poolId)
            .Select(tp => new { tp.Id, tp.Code, tp.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (pool is null)
        {
            return new TopicPoolStatisticsDto
            {
                PoolId = poolId,
                PoolCode = string.Empty,
                PoolName = string.Empty,
            };
        }

        // Query all projects in this pool
        var poolProjects = _context.Projects
            .AsNoTracking()
            .Where(p => p.TopicPoolId == poolId && p.SourceType == ProjectSourceType.FromPool);

        var statusCounts = await poolProjects
            .Where(p => p.PoolStatus.HasValue)
            .GroupBy(p => p.PoolStatus!.Value)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var totalTopics = statusCounts.Values.Sum();
        var activeTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Available);
        var registeredTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Reserved)
                             + statusCounts.GetValueOrDefault(PoolTopicStatus.Assigned);
        var expiredTopics = statusCounts.GetValueOrDefault(PoolTopicStatus.Expired);

        var totalMentors = await poolProjects
            .SelectMany(p => p.Mentors.Where(m => m.Status == ProjectMentorStatus.Active))
            .Select(m => m.MentorId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new TopicPoolStatisticsDto
        {
            PoolId = pool.Id,
            PoolCode = pool.Code,
            PoolName = pool.Name,
            TotalMentors = totalMentors,
            TotalTopicsCount = totalTopics,
            ActiveTopicsCount = activeTopics,
            RegisteredTopicsCount = registeredTopics,
            ExpiredTopicsCount = expiredTopics,
        };
    }

}
