using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SemesterAggregate
{
    public interface ISemesterRepository : IRepository<Semester, int>
    {
        Task<Semester?> GetByCodeAsync(SemesterCode code, CancellationToken cancellationToken = default);
        Task<Semester?> GetWithPhasesAsync(int id, CancellationToken cancellationToken = default);
        Task<Semester?> GetActiveAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetNextSemesterAsync(int? semesterId, CancellationToken cancellationToken);
        Task<IEnumerable<Semester>> GetByAcademicYearAsync(string academicYear, CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetUpcomingAsync(CancellationToken cancellationToken = default);
        Task<Semester?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken cancellationToken = default);
        Task<bool> ExistsCodeAsync(SemesterCode code, CancellationToken cancellationToken = default);
        Task<bool> HasOverlappingAsync(DateTime startDate, DateTime endDate, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Semester>> GetSemestersWithPhaseStartingInAsync(int days, CancellationToken cancellationToken = default);
    }
}
