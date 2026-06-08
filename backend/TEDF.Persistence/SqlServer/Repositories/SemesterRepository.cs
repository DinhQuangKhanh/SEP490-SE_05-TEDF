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

        public async Task<Semester?> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var spec = new ActiveSemesterSpec();
            return await FirstOrDefaultAsync(spec, cancellationToken);
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
