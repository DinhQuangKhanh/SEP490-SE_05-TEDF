using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Specifications.Semesters;
using TEDF.Persistence.Common;

namespace TEDF.Persistence.SqlServer.Repositories
{
    /// <summary>
    /// Repository implementation for Semester aggregate using specifications.
    /// </summary>
    public class SemesterRepository : BaseRepository<Semester, int>, ISemesterRepository
    {
        public SemesterRepository(AppDbContext context) : base(context) { }

        public override async Task<Semester?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Phases)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Semester?> GetByCodeAsync(SemesterCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Phases)
                .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
        }

        public async Task<Semester?> GetWithPhasesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Phases.OrderBy(p => p.Order))
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Semester?> GetWithRosterAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Phases.OrderBy(p => p.Order))
                .Include(s => s.EligibleStudents)
                .Include(s => s.EligibleMentors)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Semester?> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var spec = new ActiveSemesterSpec();
            return await FirstOrDefaultAsync(spec, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Semester?> GetRegistrationTargetSemesterAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Narrow to semesters that have not ended — the target is always one of those — then let
            // the domain policy pick. The phase-based rule cannot be expressed as a single indexable
            // predicate, so the ranking stays in memory over a handful of rows.
            var candidates = await _dbSet
                .Include(s => s.Phases)
                .Where(s => s.EndDate >= now)
                .ToListAsync(cancellationToken);

            return RegistrationTargetSemesterPolicy.Resolve(candidates, now);
        }

        public async Task<IEnumerable<Semester>> GetByAcademicYearAsync(string academicYear, CancellationToken cancellationToken = default)
        {
            var spec = new SemesterByAcademicYearSpec(academicYear);
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<IEnumerable<Semester>> GetUpcomingAsync(CancellationToken cancellationToken = default)
        {
            var spec = new UpcomingSemestersSpec();
            return await ListAsync(spec, cancellationToken);
        }

        public async Task<bool> ExistsCodeAsync(SemesterCode code, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(s => s.Code == code, cancellationToken);
        }

        public async Task<bool> HasOverlappingAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(s => s.StartDate < endDate && s.EndDate > startDate);
            if (excludeId.HasValue)
                query = query.Where(s => s.Id != excludeId.Value);
            return await query.AnyAsync(cancellationToken);
        }

        public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
        {
            var maxId = await _dbSet.MaxAsync(s => (int?)s.Id, cancellationToken);
            return (maxId ?? 0) + 1;
        }

        public async Task<bool> IsStudentEligibleNowAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            // Eligible on the active or any upcoming semester (i.e. one that hasn't ended yet).
            return await _context.EligibleStudents
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsEligible)
                .Join(_context.Semesters.AsNoTracking(), e => e.SemesterId, s => s.Id, (e, s) => s.EndDate)
                .AnyAsync(endDate => endDate >= now, cancellationToken);
        }

        public async Task<bool> IsStudentEligibleAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.EligibleStudents
                .AsNoTracking()
                .AnyAsync(e => e.StudentId == studentId && e.SemesterId == semesterId && e.IsEligible, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Semester?> GetEligibleSemesterForStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Earliest first: a student rostered on both the running semester and the next one belongs
            // to the running one — they only move on once it has ended.
            return await _context.EligibleStudents
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsEligible)
                .Join(_context.Semesters, e => e.SemesterId, s => s.Id, (e, s) => s)
                .Where(s => s.EndDate >= now)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> IsMentorEligibleNowAsync(Guid mentorId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            // Eligible on the active or any upcoming semester (i.e. one that hasn't ended yet).
            return await _context.EligibleMentors
                .AsNoTracking()
                .Where(m => m.MentorId == mentorId && m.IsAssigned)
                .Join(_context.Semesters.AsNoTracking(), m => m.SemesterId, s => s.Id, (m, s) => s.EndDate)
                .AnyAsync(endDate => endDate >= now, cancellationToken);
        }

        public async Task<int?> GetEligibleStudentMajorAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default)
        {
            return await _context.EligibleStudents
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.SemesterId == semesterId && e.IsEligible)
                .Select(e => e.MajorId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetEligibleMentorIdsByMajorAsync(int semesterId, int majorId, CancellationToken cancellationToken = default)
        {
            return await _context.EligibleMentors
                .AsNoTracking()
                .Where(m => m.SemesterId == semesterId && m.MajorId == majorId && m.IsAssigned)
                .Select(m => m.MentorId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TEDF.Domain.Aggregates.SemesterAggregate.Entities.EligibleMentor>> GetEligibleMentorsByMajorAsync(
            int semesterId, int majorId, CancellationToken cancellationToken = default)
        {
            return await _context.EligibleMentors
                .AsNoTracking()
                .Where(m => m.SemesterId == semesterId && m.MajorId == majorId && m.IsAssigned)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Semester>> GetSemestersWithPhaseStartingInAsync(int days, CancellationToken cancellationToken = default)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(days);
            return await _dbSet
                .Include(s => s.Phases)
                .Where(s => s.Phases.Any(p => p.StartDate.Date == targetDate))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all semesters with their phases.
        /// </summary>
        public override async Task<IEnumerable<Semester>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Phases)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the semester that is N semesters after the given semester.
        /// </summary>
        public async Task<Semester?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default)
        {
            var currentSemester = await _dbSet.FindAsync([semesterId], cancellationToken);
            if (currentSemester == null) return null;

            return await _dbSet
                .Where(s => s.StartDate > currentSemester.EndDate)
                .OrderBy(s => s.StartDate)
                .Skip(count - 1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Semester?> GetPreviousSemesterAsync(int semesterId, CancellationToken cancellationToken = default)
        {
            var currentSemester = await _dbSet.FindAsync([semesterId], cancellationToken);
            if (currentSemester == null) return null;

            // Mirror of GetSemesterAfterAsync: semesters are ordered by their date range, not by Id,
            // because Ids are assigned by creation order and a semester can be created out of order.
            return await _dbSet
                .Where(s => s.EndDate < currentSemester.StartDate)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Semester?> GetNextSemesterAsync(int? semesterId, CancellationToken cancellationToken)
        {
            if (semesterId.HasValue) return
                    await _context.Semesters
                    .AsNoTracking()
                    .Where(s => s.Id == semesterId)
                    .FirstOrDefaultAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var activeSemester = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.StartDate <= now && s.EndDate >= now)
                .Select(s => new { s.Id, s.EndDate })
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSemester is null) return null;

            // Find the next semester that starts after the current one ends
            return await _context.Semesters
                .AsNoTracking()
                .Where(s => s.StartDate > activeSemester.EndDate)
                .OrderBy(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
