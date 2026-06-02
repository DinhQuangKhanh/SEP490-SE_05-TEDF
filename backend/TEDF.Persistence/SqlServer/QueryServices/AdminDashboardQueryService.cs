using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.Ticket;
using TEDF.Persistence.SqlServer;

namespace TEDF.Persistence.SqlServer.QueryServices;

public class AdminDashboardQueryService : IAdminDashboardQueryService
{
    private readonly AppDbContext _context;

    public AdminDashboardQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        // 1. User counts by role
        var totalStudents = await _context.UserRoles
            .AsNoTracking()
            .CountAsync(r => r.RoleName == "Student" && r.IsActive, cancellationToken);

        var totalMentors = await _context.UserRoles
            .AsNoTracking()
            .CountAsync(r => r.RoleName == "Mentor" && r.IsActive, cancellationToken);

        // 2. Active semester with phases
        var now = DateTime.UtcNow;
        var activeSemester = await _context.Semesters
            .AsNoTracking()
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(
                s => s.StartDate <= now && s.EndDate >= now,
                cancellationToken);

        // 3. Project stats for active semester
        var totalRegisteredTopics = 0;
        var approvalRate = new ApprovalRateDto();

        if (activeSemester != null)
        {
            totalRegisteredTopics = await _context.Projects
                .AsNoTracking()
                .CountAsync(p => p.SemesterId == activeSemester.Id, cancellationToken);

            var statusCounts = await _context.Projects
                .AsNoTracking()
                .Where(p => p.SemesterId == activeSemester.Id)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var approved = statusCounts
                .Where(s => s.Status == ProjectStatus.Approved)
                .Sum(s => s.Count);
            var rejected = statusCounts
                .Where(s => s.Status == ProjectStatus.Rejected)
                .Sum(s => s.Count);
            var inProgress = statusCounts
                .Where(s => s.Status == ProjectStatus.InProgress)
                .Sum(s => s.Count);
            var pending = statusCounts
                .Where(s => s.Status is ProjectStatus.Draft
                    or ProjectStatus.PendingEvaluation
                    or ProjectStatus.NeedsModification)
                .Sum(s => s.Count);

            approvalRate = new ApprovalRateDto
            {
                Approved = approved,
                Rejected = rejected,
                InProgress = inProgress,
                Pending = pending,
                Total = totalRegisteredTopics,
            };
        }

        // 4. High priority pending support tickets (Open or InProgress, High or Urgent)
        var highPriorityPending = await _context.SupportTickets
            .AsNoTracking()
            .CountAsync(t =>
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) &&
                (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Urgent),
                cancellationToken);

        // 5. Semester progress
        SemesterProgressDto? semesterProgress = null;
        if (activeSemester != null)
        {
            semesterProgress = new SemesterProgressDto
            {
                SemesterName = activeSemester.Name,
                Phases = activeSemester.Phases
                    .OrderBy(p => p.Order)
                    .Select(p => new SemesterPhaseDto
                    {
                        Name = p.Name,
                        Type = (int)p.Type,
                        Status = (int)p.Status,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Order = p.Order,
                    })
                    .ToList(),
            };
        }

        // 6. Recent 5 support tickets with reporter names
        var recentTickets = await (
            from t in _context.SupportTickets.AsNoTracking()
            join u in _context.Users.AsNoTracking() on t.ReporterId equals u.Id
            orderby t.CreatedAt descending
            select new RecentTicketDto
            {
                Code = t.Code,
                Title = t.Title,
                ReporterName = u.FullName,
                Category = (int)t.Category,
                Priority = (int)t.Priority,
                Status = (int)t.Status,
                CreatedAt = t.CreatedAt,
            })
            .Take(5)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            Stats = new AdminStatsDto
            {
                TotalStudents = totalStudents,
                TotalMentors = totalMentors,
                TotalRegisteredTopics = totalRegisteredTopics,
                HighPriorityPending = highPriorityPending,
            },
            SemesterProgress = semesterProgress,
            ApprovalRate = approvalRate,
            RecentTickets = recentTickets,
        };
    }
}
