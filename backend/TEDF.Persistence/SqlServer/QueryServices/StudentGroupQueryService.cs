using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
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
public class StudentGroupQueryService : IStudentGroupQueryService
{
    private readonly AppDbContext _context;

    public StudentGroupQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MentorGroupDto>> GetMentorGroupsAsync(
        Guid mentorId,
        int? semesterId,
        CancellationToken cancellationToken = default)
    {
        var targetSemesterId = await ResolveSemesterIdAsync(semesterId, cancellationToken);
        if (targetSemesterId == 0) return [];

        var groups = await (
            from pm in _context.ProjectMentors.AsNoTracking()
            where pm.MentorId == mentorId && pm.Status == ProjectMentorStatus.Active
            join p in _context.Projects on pm.ProjectId equals p.Id
            where p.SemesterId == targetSemesterId && p.GroupId != null
                  && (p.Status == ProjectStatus.Approved
                   || p.Status == ProjectStatus.InProgress
                   || p.Status == ProjectStatus.Completed)
            join g in _context.Groups on p.GroupId equals g.Id
            select new MentorGroupDto
            {
                GroupId = g.Id,
                GroupCode = g.Code,
                GroupName = g.Name,
                GroupStatus = g.Status.ToString(),
                MaxMembers = g.MaxMembers,
                ProjectId = p.Id,
                ProjectName = p.NameVi,
                ProjectCode = p.Code,
                ProjectStatus = p.Status.ToString(),
                CreatedAt = g.CreatedAt,
                Members = (
                    from gm in _context.GroupMembers
                    where gm.GroupId == g.Id
                    join u in _context.Users on gm.StudentId equals u.Id
                    select new GroupMemberDto
                    {
                        StudentId = u.Id,
                        FullName = u.FullName,
                        StudentCode = u.StudentCode,
                        Email = u.Email,
                        Role = gm.Role.ToString(),
                        Status = gm.Status.ToString(),
                        JoinedAt = gm.JoinedAt
                    }
                ).ToList()
            }
        ).ToListAsync(cancellationToken);

        return groups;
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
                MaxMembers = g.MaxMembers,
                IsOpenForRequests = g.IsOpenForRequests,
                ProjectId = g.ProjectId,
                // Project is optional for newly created groups, so project fields must be null-safe.
                ProjectName = p == null ? null : EF.Property<string>(p, "NameVi"),
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
                StudentCode = u.StudentCode,
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
            GroupStatus = groupData.GroupStatus,
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
        // Open groups are always from the NEXT semester (after the currently active one)
        var targetSemesterId = await ResolveNextSemesterIdAsync(semesterId, cancellationToken);
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
                    StudentCode = u.StudentCode,
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
            join u in _context.Users on r.StudentId equals u.Id
            select new JoinRequestDto
            {
                Id = r.Id,
                StudentId = u.Id,
                StudentName = u.FullName,
                StudentCode = u.StudentCode,
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
        // Pending join requests target groups from the NEXT semester
        var targetSemesterId = await ResolveNextSemesterIdAsync(semesterId, cancellationToken);
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

    private async Task<int> ResolveSemesterIdAsync(int? semesterId, CancellationToken cancellationToken)
    {
        return semesterId
            ?? await _context.Semesters
                .AsNoTracking()
                .Where(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the next semester after the currently active one.
    /// Used for open groups and pending join requests — students browse/join groups for the upcoming semester.
    /// If semesterId is explicitly provided, uses it directly. Otherwise finds the next semester automatically.
    /// Returns 0 if no next semester exists (admin hasn't created one yet).
    /// </summary>
    private async Task<int> ResolveNextSemesterIdAsync(int? semesterId, CancellationToken cancellationToken)
    {
        if (semesterId.HasValue) return semesterId.Value;

        var now = DateTime.UtcNow;

        var activeSemester = await _context.Semesters
            .AsNoTracking()
            .Where(s => s.StartDate <= now && s.EndDate >= now)
            .Select(s => new { s.Id, s.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSemester is null) return 0;

        // Find the next semester that starts after the current one ends
        return await _context.Semesters
            .AsNoTracking()
            .Where(s => s.StartDate > activeSemester.EndDate)
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
            from u in _context.Users.AsNoTracking()
            where u.StudentCode != null
                  && u.Status == UserStatus.Active
                  && !studentsInGroups.Contains(u.Id)
                  && _context.UserRoles.Any(r => r.UserId == u.Id && r.RoleName == "Student" && r.IsActive)
            orderby u.StudentCode
            select new AvailableStudentDto(u.Id, u.StudentCode!, u.FullName)
        ).ToListAsync(cancellationToken);
    }
}
