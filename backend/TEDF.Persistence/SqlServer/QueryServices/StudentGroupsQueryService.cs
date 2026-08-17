using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.StudentGroups;
using TEDF.Application.Features.StudentGroups.DTOs;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Mentor;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.Semester;
using TEDF.Domain.Enums.User;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// EF Core implementation of complex StudentGroup read queries.
/// Uses direct DbContext access with AsNoTracking and Select projections for optimal performance.
/// </summary>
public class StudentGroupsQueryService : IStudentGroupsQueryService
{
    private readonly AppDbContext _context;

    public StudentGroupsQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MentorGroupDto>> GetMentorGroupsAsync(
        Guid mentorId,
        int? semesterId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var groups = await (
            from pm in _context.ProjectMentors.AsNoTracking()
            where pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active
            join p in _context.Projects on pm.ProjectId equals p.Id
            // PendingEvaluation is included so a mentor keeps seeing a group whose topic is still
            // under review. The `p.GroupId != null` gate keeps a mentor's own *unassigned* pool
            // proposal (also PendingEvaluation, but GroupId == null) out — a pool topic only gets a
            // GroupId at registration-confirm, or when its proposed roster becomes a group on approval.
            where p.GroupId != null
                  && (p.Status == ProjectStatus.PendingEvaluation
                   || p.Status == ProjectStatus.Approved
                   || p.Status == ProjectStatus.InProgress
                   || p.Status == ProjectStatus.Completed)
            join g in _context.Groups on p.GroupId equals g.Id
            join s in _context.Semesters on g.SemesterId equals s.Id
            // Filter by the GROUP's semester, not the project's. For a topic registered from the
            // pool, project.SemesterId is the semester the mentor PROPOSED the topic, which differs
            // from the registering group's semester; keying on the group aligns pool & direct flows.
            // When no semester is chosen, show groups in the active OR any upcoming semester
            // (EndDate ≥ now) — a group is registered for the upcoming term, so restricting to the
            // current-active semester alone would hide freshly approved groups.
            where semesterId.HasValue ? g.SemesterId == semesterId.Value : s.EndDate >= now
            select new MentorGroupDto
            {
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                GroupStatus = g.Status.ToString(),
                MaxMembers = g.MaxMembers,
                ProjectId = p.Id,
                ProjectName = p.NameVi,
                ProjectNameEn = p.NameEn,
                ProjectCode = p.Code,
                ProjectStatus = p.Status.ToString(),
                SemesterId = s.Id,
                SemesterName = s.Name,
                SemesterStartDate = s.StartDate,
                CreatedAt = g.CreatedAt,
                Members = (
                    from gm in _context.GroupMembers.AsNoTracking()
                    // Current members only. Leaving a group is a soft status change (Status → Left,
                    // the row is kept), so without this filter the list returned every past membership
                    // — a student who left and rejoined showed up twice, and "Left" rows inflated the
                    // count past MaxMembers. Mirrors GetStudentGroupAsync / GetOpenGroupsAsync.
                    where gm.GroupId == g.Id && gm.Status == GroupMemberStatus.Active
                    join u in _context.Users on gm.StudentId equals u.Id
                    select new GroupMemberDto
                    {
                        StudentId = u.Id,
                        FullName = u.FullName,
                        StudentCode = u.Student != null ? u.Student.StudentCode : null,
                        Email = u.Email,
                        Role = gm.Role.ToString(),
                        Status = gm.Status.ToString(),
                        JoinedAt = gm.JoinedAt
                    }
                ).ToList()
            }
        ).ToListAsync(cancellationToken);

        // Every group here is supervised by this mentor, so their name is fetched once rather than
        // joined per row.
        var mentorName = await _context.Users.AsNoTracking()
            .Where(u => u.Id == mentorId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return groups
            .Select(g => g with
            {
                DisplayName = GroupNameFormatter.Build(
                    g.GroupName, g.GroupCode, g.ProjectNameEn, mentorName, g.ProjectStatus)
            })
            .ToList();
    }

    public async Task<StudentGroupDto?> GetStudentGroupAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // The student's thesis group belongs to the semester they do their thesis in. Registration
        // happens during the PREVIOUS semester, so that group may sit in either the currently
        // active semester or an upcoming one. We therefore resolve the semester from the student's
        // own active group (keeping the nearest current-or-upcoming semester) instead of blindly
        // taking the semester AFTER the active one — which broke once the thesis semester itself
        // became active (it then pointed at the semester after that, finding nothing).
        var groupData = await (
            from gm in _context.GroupMembers.AsNoTracking()
            where gm.StudentId == studentId && gm.Status == GroupMemberStatus.Active
            join g in _context.Groups.AsNoTracking() on gm.GroupId equals g.Id
            where g.Status == GroupStatus.Active
            join sem in _context.Semesters.AsNoTracking() on g.SemesterId equals sem.Id
            where sem.EndDate >= now // current or upcoming semester only (exclude ended ones)
            join p in _context.Projects.AsNoTracking() on g.ProjectId equals p.Id into projectJoin
            from p in projectJoin.DefaultIfEmpty()
            orderby sem.StartDate // prefer the active semester over a later upcoming one
            select new
            {
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                GroupStatus = g.Status.ToString(),
                IsLeader = g.LeaderId == studentId,
                MaxMembers = g.MaxMembers,
                IsOpenForRequests = g.IsOpenForRequests,
                ProjectId = g.ProjectId,
                // Project is optional for newly created groups, so project fields must be null-safe.
                ProjectName = p == null ? null : EF.Property<string>(p, "NameVi"),
                ProjectNameEn = p == null ? null : EF.Property<string>(p, "NameEn"),
                ProjectCode = p == null ? null : EF.Property<string>(p, "Code"),
                ProjectStatus = p != null ? p.Status.ToString() : null,
                CreatedAt = g.CreatedAt,
                ProjectMentorName = p != null
                    ? (from pm in _context.ProjectMentors.AsNoTracking()
                       where pm.ProjectId == p.Id && pm.Status == ProjectMentorStatus.Active
                       join u in _context.Users.AsNoTracking() on pm.MentorId equals u.Id
                       select u.FullName).FirstOrDefault()
                    : null
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (groupData is null) return null;

        var members = await (
            from m in _context.GroupMembers.AsNoTracking()
            where m.GroupId == groupData.GroupId && m.Status == GroupMemberStatus.Active
            join u in _context.Users.AsNoTracking() on m.StudentId equals u.Id
            select new GroupMemberDto
            {
                StudentId = u.Id,
                FullName = u.FullName,
                StudentCode = u.Student != null ? u.Student.StudentCode : null,
                Email = u.Email,
                Role = m.Role.ToString(),
                Status = m.Status.ToString(),
                JoinedAt = m.JoinedAt
            }
        ).ToListAsync(cancellationToken);

        return new StudentGroupDto
        {
            GroupId = groupData.GroupId,
            GroupCode = groupData.GroupCode,
            GroupName = groupData.GroupName,
            DisplayName = GroupNameFormatter.Build(
                groupData.GroupName, groupData.GroupCode, groupData.ProjectNameEn,
                groupData.ProjectMentorName, groupData.ProjectStatus),
            GroupStatus = groupData.GroupStatus,
            IsLeader = groupData.IsLeader,
            MaxMembers = groupData.MaxMembers,
            IsOpenForRequests = groupData.IsOpenForRequests,
            ProjectId = groupData.ProjectId,
            ProjectName = groupData.ProjectName,
            ProjectCode = groupData.ProjectCode,
            ProjectStatus = groupData.ProjectStatus,
            MentorName = groupData.ProjectMentorName,
            CreatedAt = groupData.CreatedAt,
            Members = members
        };
    }

    public async Task<List<OpenGroupDto>> GetOpenGroupsAsync(
        Guid studentId,
        int? semesterId,
        CancellationToken cancellationToken = default)
    {
        // Open groups come from the semester this student is rostered for.
        var targetSemesterId = await ResolveNextSemesterIdAsync(semesterId, studentId, cancellationToken);
        if (targetSemesterId == 0) return [];

        var groups = await _context.Groups.AsNoTracking()
            .Where(g => g.SemesterId == targetSemesterId
                     && g.Status == GroupStatus.Active
                     && g.IsOpenForRequests
                     // Exclude groups where the student is already an active member
                     && !_context.GroupMembers.Any(m => m.GroupId == g.Id && m.StudentId == studentId && m.Status == GroupMemberStatus.Active))
            .Select(g => new
            {
                g.Id,
                g.Code,
                g.Name,
                g.MaxMembers,
                g.CreatedAt,
                ActiveMemberCount = _context.GroupMembers.Count(m => m.GroupId == g.Id && m.Status == GroupMemberStatus.Active)
            })
            .Where(x => x.ActiveMemberCount < x.MaxMembers)
            .ToListAsync(cancellationToken);

        if (groups.Count == 0) return [];

        var groupIds = groups.Select(g => g.Id).ToList();

        var memberRows = await (
            from m in _context.GroupMembers.AsNoTracking()
            where groupIds.Contains(m.GroupId) && m.Status == GroupMemberStatus.Active
            join u in _context.Users.AsNoTracking() on m.StudentId equals u.Id
            select new
            {
                m.GroupId,
                Member = new GroupMemberDto
                {
                    StudentId = u.Id,
                    FullName = u.FullName,
                    StudentCode = u.Student != null ? u.Student.StudentCode : null,
                    Email = u.Email,
                    Role = m.Role.ToString(),
                    Status = m.Status.ToString(),
                    JoinedAt = m.JoinedAt
                }
            }
        ).ToListAsync(cancellationToken);

        var membersByGroupId = memberRows
            .GroupBy(x => x.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Member).ToList());

        return groups
            .Select(g => new OpenGroupDto
            {
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                MemberCount = g.ActiveMemberCount,
                MaxMembers = g.MaxMembers,
                CreatedAt = g.CreatedAt,
                Members = membersByGroupId.TryGetValue(g.Id, out var members) ? members : []
            })
            .ToList();
    }

    public async Task<List<InvitationDto>> GetStudentInvitationsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var invitations = await (
            from i in _context.GroupInvitations.AsNoTracking()
            where i.InviteeId == studentId && i.Status == GroupInvitationStatus.Pending
            join g in _context.Groups on i.GroupId equals g.Id
            join inviter in _context.Users on i.InviterId equals inviter.Id
            where i.ExpiresAt > DateTime.UtcNow
            select new InvitationDto
            {
                Id = i.Id,
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                InviterId = inviter.Id,
                InviterName = inviter.FullName,
                Message = i.Message,
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt,
                ExpiresAt = i.ExpiresAt
            }
        ).ToListAsync(cancellationToken);

        return invitations;
    }

    public async Task<List<JoinRequestDto>> GetGroupJoinRequestsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var requests = await (
            from r in _context.GroupJoinRequests.AsNoTracking()
            where r.GroupId == groupId
               && r.Status == GroupJoinRequestStatus.Pending
               && r.ExpiresAt > now
            join u in _context.Users.AsNoTracking() on r.StudentId equals u.Id
            select new JoinRequestDto
            {
                Id = r.Id,
                StudentId = u.Id,
                StudentName = u.FullName,
                StudentCode = u.Student != null ? u.Student.StudentCode : null,
                Message = r.Message,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            }
        ).ToListAsync(cancellationToken);

        return requests;
    }

    public async Task<PendingJoinRequestDto?> GetStudentPendingJoinRequestAsync(
        Guid studentId,
        int? semesterId,
        CancellationToken cancellationToken = default)
    {
        // Pending join requests target groups from the semester this student is rostered for.
        var targetSemesterId = await ResolveNextSemesterIdAsync(semesterId, studentId, cancellationToken);
        if (targetSemesterId == 0) return null;

        var now = DateTime.UtcNow;

        return await (
            from r in _context.GroupJoinRequests.AsNoTracking()
            where r.StudentId == studentId
               && r.Status == GroupJoinRequestStatus.Pending
               && r.ExpiresAt > now
            join g in _context.Groups.AsNoTracking() on r.GroupId equals g.Id
            where g.SemesterId == targetSemesterId && g.Status == GroupStatus.Active
            orderby r.CreatedAt descending
            select new PendingJoinRequestDto
            {
                RequestId = r.Id,
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                Message = r.Message,
                CreatedAt = r.CreatedAt,
                ExpiresAt = r.ExpiresAt
            }
        ).FirstOrDefaultAsync(cancellationToken);
    }


    /// <summary>
    /// Resolves the semester whose groups this student browses and joins: the earliest semester that
    /// has not ended yet and carries the student on its eligible roster.
    /// If semesterId is explicitly provided, uses it directly. Returns 0 when the student is not on
    /// any current or upcoming roster.
    /// <para>
    /// Must stay in step with <c>ISemesterRepository.GetEligibleSemesterForStudentAsync</c>, which
    /// group creation uses — if the two picked different semesters, a student would create a group
    /// they could then not see in the open-group list.
    /// </para>
    /// </summary>
    private async Task<int> ResolveNextSemesterIdAsync(int? semesterId, Guid studentId, CancellationToken cancellationToken)
    {
        if (semesterId.HasValue) return semesterId.Value;

        var now = DateTime.UtcNow;

        return await _context.EligibleStudents
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.IsEligible)
            .Join(_context.Semesters.AsNoTracking(), e => e.SemesterId, s => s.Id, (e, s) => s)
            .Where(s => s.EndDate >= now)
            .OrderBy(s => s.StartDate)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AvailableStudentDto>> GetInvitableStudentsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        // Resolve the group's semester.
        var semesterId = await _context.Groups.AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => (int?)g.SemesterId)
            .FirstOrDefaultAsync(cancellationToken);
        if (semesterId is null) return [];

        // Students already in an active group this semester are excluded.
        var studentsInGroups =
            from gm in _context.GroupMembers.AsNoTracking()
            where gm.Status == GroupMemberStatus.Active
            join g in _context.Groups on gm.GroupId equals g.Id
            where g.SemesterId == semesterId && g.Status == GroupStatus.Active
            select gm.StudentId;

        return await (
            from s in _context.Students.AsNoTracking()
            join u in _context.Users.AsNoTracking() on s.Id equals u.Id
            where u.Status == UserStatus.Active
                  && !studentsInGroups.Contains(u.Id)
                  && _context.UserRoles.Any(r => r.UserId == u.Id && r.RoleId == 3 && r.IsActive)
                  // Only students on this semester's eligible roster may be invited into a group.
                  && _context.EligibleStudents.Any(e =>
                        e.StudentId == u.Id && e.SemesterId == semesterId && e.IsEligible)
            orderby s.StudentCode
            select new AvailableStudentDto(u.Id, s.StudentCode, u.FullName)
        ).ToListAsync(cancellationToken);
    }
}
