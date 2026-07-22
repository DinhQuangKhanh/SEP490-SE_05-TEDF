using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SemesterAggregate
{
    public interface ISemesterRepository : IRepository<Semester, int>
    {
        Task<Semester?> GetByCodeAsync(SemesterCode code, CancellationToken cancellationToken = default);
        Task<Semester?> GetWithPhasesAsync(int id, CancellationToken cancellationToken = default);
        /// <summary>Loads a semester with phases and the full eligibility roster (students + mentors) tracked for writes.</summary>
        Task<Semester?> GetWithRosterAsync(int id, CancellationToken cancellationToken = default);
        Task<Semester?> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetNextSemesterAsync(int? semesterId, CancellationToken cancellationToken);
        Task<IEnumerable<Semester>> GetByAcademicYearAsync(string academicYear, CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetUpcomingAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default);
        Task<bool> ExistsCodeAsync(SemesterCode code, CancellationToken cancellationToken = default);
        Task<bool> HasOverlappingAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetSemestersWithPhaseStartingInAsync(int days, CancellationToken cancellationToken = default);
        /// <summary>True if the student is on the IsEligible list of any active or upcoming semester (EndDate ≥ now).</summary>
        Task<bool> IsStudentEligibleNowAsync(Guid studentId, CancellationToken cancellationToken = default);

        /// <summary>True if the student is on the IsEligible roster of the given semester.</summary>
        Task<bool> IsStudentEligibleAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>True if the mentor is on the IsAssigned eligible-mentor roster of any active or upcoming semester (EndDate ≥ now).</summary>
        Task<bool> IsMentorEligibleNowAsync(Guid mentorId, CancellationToken cancellationToken = default);

        /// <summary>The student's assigned program (Major) on the eligible-student roster of the given semester, or null if not rostered / not yet assigned.</summary>
        Task<int?> GetEligibleStudentMajorAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>Ids of mentors assigned to supervise the given major on the eligible-mentor roster of the given semester.</summary>
        Task<List<Guid>> GetEligibleMentorIdsByMajorAsync(int semesterId, int majorId, CancellationToken cancellationToken = default);
    }
}
