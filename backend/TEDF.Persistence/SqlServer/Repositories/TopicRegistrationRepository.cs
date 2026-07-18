using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.TopicPool;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories;

/// <summary>
/// Repository implementation for TopicRegistration entity.
/// </summary>
public class TopicRegistrationRepository : BaseRepository<TopicRegistration, Guid>, ITopicRegistrationRepository
{
    public TopicRegistrationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<TopicRegistration>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tr => tr.ProjectId == projectId)
            .OrderBy(tr => tr.Priority)
            .ThenBy(tr => tr.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TopicRegistration>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tr => tr.GroupId == groupId)
            .OrderByDescending(tr => tr.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TopicRegistration>> GetPendingByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tr => tr.ProjectId == projectId && tr.Status == TopicRegistrationStatus.Pending)
            .OrderBy(tr => tr.Priority)
            .ThenBy(tr => tr.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPendingRegistrationAsync(Guid groupId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            tr => tr.GroupId == groupId &&
                  tr.ProjectId == projectId &&
                  tr.Status == TopicRegistrationStatus.Pending,
            cancellationToken);
    }

    public async Task<TopicRegistration?> GetConfirmedByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(tr => tr.ProjectId == projectId && tr.Status == TopicRegistrationStatus.Confirmed, cancellationToken);
    }

    public async Task<IEnumerable<TopicRegistration>> GetConfirmedByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tr => tr.GroupId == groupId && tr.Status == TopicRegistrationStatus.Confirmed)
            .OrderByDescending(tr => tr.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TopicRegistration>> GetPendingByMentorIdAsync(Guid mentorId, CancellationToken cancellationToken = default)
    {
        // Get pending registrations for projects where this mentor is active
        return await _dbSet
            .Join(_context.Projects,
                tr => tr.ProjectId,
                p => p.Id,
                (tr, p) => new { Registration = tr, Project = p })
            .Where(x => x.Registration.Status == TopicRegistrationStatus.Pending &&
                       x.Project.Mentors.Any(m => m.MentorId == mentorId && m.Status == ProjectMentorStatus.Active))
            .Select(x => x.Registration)
            .OrderBy(tr => tr.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPendingByProjectIdExcludingAsync(Guid projectId, Guid excludeRegistrationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(tr => tr.ProjectId == projectId &&
                             tr.Status == TopicRegistrationStatus.Pending &&
                             tr.Id != excludeRegistrationId, cancellationToken);
    }

    public async Task<int> CountPendingByMentorIdAsync(Guid mentorId, CancellationToken cancellationToken = default)
    {
        // Correlated EXISTS subquery; use the mapped Status (not the computed IsActive, which EF
        // cannot translate) so the predicate runs in SQL.
        return await _dbSet.CountAsync(
            tr => tr.Status == TopicRegistrationStatus.Pending &&
                  _context.Projects.Any(p => p.Id == tr.ProjectId &&
                      p.Mentors.Any(m => m.MentorId == mentorId && m.Status == ProjectMentorStatus.Active)),
            cancellationToken);
    }

    public async Task<Dictionary<TopicRegistrationStatus, int>> GetRegistrationStatusCountsByProjectIdsAsync(IEnumerable<Guid> projectIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(tr => projectIds.Contains(tr.ProjectId))
            .GroupBy(tr => tr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }
}
