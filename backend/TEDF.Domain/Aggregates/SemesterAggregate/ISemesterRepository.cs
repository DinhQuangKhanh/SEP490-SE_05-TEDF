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

        /// <summary>
        /// The semester a topic proposed right now will be carried out in — see
        /// <see cref="RegistrationTargetSemesterPolicy"/>. This is deliberately <b>not</b>
        /// <see cref="GetActiveAsync"/>: the Registration/Evaluation phases of semester N run during
        /// semester N-1, so the running semester is never the one a new topic belongs to.
        /// </summary>
        Task<Semester?> GetRegistrationTargetSemesterAsync(CancellationToken cancellationToken = default);

        Task<Semester?> GetNextSemesterAsync(int? semesterId, CancellationToken cancellationToken);
        Task<IEnumerable<Semester>> GetByAcademicYearAsync(string academicYear, CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetUpcomingAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// The semester immediately preceding the given one — the latest semester that ended before
        /// it starts — or null when it is the earliest semester on record.
        /// </summary>
        Task<Semester?> GetPreviousSemesterAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<bool> ExistsCodeAsync(SemesterCode code, CancellationToken cancellationToken = default);
        Task<bool> HasOverlappingAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetSemestersWithPhaseStartingInAsync(int days, CancellationToken cancellationToken = default);
        /// <summary>True if the student is on the IsEligible list of any active or upcoming semester (EndDate ≥ now).</summary>
        Task<bool> IsStudentEligibleNowAsync(Guid studentId, CancellationToken cancellationToken = default);

        /// <summary>True if the student is on the IsEligible roster of the given semester.</summary>
        Task<bool> IsStudentEligibleAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The semester the student is supposed to be working in: the earliest semester that has not
        /// ended yet (active or upcoming) and carries the student on its IsEligible roster. Null when
        /// the student is not rostered on any such semester.
        /// <para>
        /// This is the counterpart of <see cref="IsStudentEligibleNowAsync"/> — same set of semesters,
        /// but it returns which one instead of a yes/no. Group creation and group browsing must anchor
        /// on this, not on <see cref="GetNextSemesterAsync"/>: the latter answers "the semester after
        /// the one running right now", which is a different thing and is null both when no semester is
        /// running and when the newest semester is itself the running one.
        /// </para>
        /// </summary>
        Task<Semester?> GetEligibleSemesterForStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

        /// <summary>True if the mentor is on the IsAssigned eligible-mentor roster of any active or upcoming semester (EndDate ≥ now).</summary>
        Task<bool> IsMentorEligibleNowAsync(Guid mentorId, CancellationToken cancellationToken = default);

        /// <summary>The student's assigned program (Major) on the eligible-student roster of the given semester, or null if not rostered / not yet assigned.</summary>
        Task<int?> GetEligibleStudentMajorAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>Ids of mentors assigned to supervise the given major on the eligible-mentor roster of the given semester.</summary>
        Task<List<Guid>> GetEligibleMentorIdsByMajorAsync(int semesterId, int majorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The eligible-mentor rows (each carrying MentorId + Email) assigned to the given major on the
        /// given semester's roster. Used to match a register form's supervisor e-mail to a published
        /// mentor, then resolve that mentor's id.
        /// </summary>
        Task<List<Entities.EligibleMentor>> GetEligibleMentorsByMajorAsync(int semesterId, int majorId, CancellationToken cancellationToken = default);
    }
}
