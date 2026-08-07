using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Specifications.Groups;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for Group aggregate.
    /// </summary>
    public class GroupRepository : BaseRepository<Group, Guid>, IGroupRepository
    {
        public GroupRepository(AppDbContext context) : base(context) { }

        public async Task<Group?> GetByCodeAsync(GroupCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(g => g.Code == code, cancellationToken);
        }

        public async Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Group>> GetBySemesterIdAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            var spec = new GroupBySemesterSpec(semesterId, includeMembers: true);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Group>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var spec = new GroupByStudentSpec(studentId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<Group?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.ProjectId == projectId, cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(GroupCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(g => g.Code == code, cancellationToken);
        }

        public async Task<int> GetNextSequenceAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: a soft-deleted group still owns its code (the unique index does
            // not filter), so its sequence must not be handed out twice.
            var codes = await _dbSet
                .IgnoreQueryFilters()
                .Where(g => g.SemesterId == semesterId)
                .Select(g => g.Code)
                .ToListAsync(cancellationToken);

            // Max, not last-by-string: "SE_100" sorts before "SE_99" lexicographically.
            var maxSequence = codes.Count == 0 ? 0 : codes.Max(c => c.Sequence);
            return maxSequence + 1;
        }

        public async Task<bool> IsStudentInActiveGroupAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(g => g.SemesterId == semesterId && g.Status == GroupStatus.Active)
                .AnyAsync(g => g.Members.Any(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Active), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> HadGroupInSemesterAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default)
        {
            // Disbanded groups are excluded on purpose: the student ended the semester without a
            // group, so nothing was consumed and they stay eligible for the next one.
            return await _dbSet
                .Where(g => g.SemesterId == semesterId && g.Status != GroupStatus.Disbanded)
                .AnyAsync(g => g.Members.Any(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Active), cancellationToken);
        }

        public async Task<bool> HasPendingJoinRequestAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.GroupJoinRequests
                .Where(r => r.StudentId == studentId
                         && r.Status == GroupJoinRequestStatus.Pending
                         && r.ExpiresAt > DateTime.UtcNow)
                .Join(_dbSet,
                    request => request.GroupId,
                    group => group.Id,
                    (request, group) => new { request, group })
                .AnyAsync(x => x.group.SemesterId == semesterId && x.group.Status == GroupStatus.Active, cancellationToken);
        }

        public async Task<bool> IsLeaderOfGroupAsync(Guid leaderId, Guid groupId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AnyAsync(g => g.Id == groupId && g.LeaderId == leaderId, cancellationToken);
        }

        /// <summary>
        /// Gets groups by leader using specification.
        /// </summary>
        public async Task<IEnumerable<Group>> GetByLeaderIdAsync(Guid leaderId, CancellationToken cancellationToken = default)
        {
            var spec = new GroupByLeaderSpec(leaderId);
            return await ListAsync(spec, cancellationToken);
        }

        /// <summary>
        /// Gets groups without project using specification.
        /// </summary>
        public async Task<IEnumerable<Group>> GetGroupsWithoutProjectAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            var spec = new GroupWithoutProjectSpec(semesterId);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<List<Guid>> GetActiveGroupIdsWithoutProjectAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(g => g.SemesterId == semesterId &&
                           g.Status == GroupStatus.Active &&
                           g.ProjectId == null)
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<Group?> GetWithInvitationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(g => g.Members)
                .Include(g => g.Invitations)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<Group?> GetWithJoinRequestsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(g => g.Members)
                .Include(g => g.JoinRequests)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<Group?> GetWithJoinRequestsAndInvitationsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _dbSet
                .Include(g => g.Members)
                .Include(g => g.Invitations)
                .Include(g => g.JoinRequests)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }

        public async Task<Group?> GetWithAllRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(g => g.Members)
                .Include(g => g.Invitations)
                .Include(g => g.JoinRequests)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        }
    }
}
