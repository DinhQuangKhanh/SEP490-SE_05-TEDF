using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices
{
    public class SemesterDomainService : ISemesterDomainService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IDateTimeService _dateTimeService;

        public SemesterDomainService(
            ISemesterRepository semesterRepository,
            IDateTimeService dateTimeService)
        {
            _semesterRepository = semesterRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<int?> GetActiveSemesterIdAsync(CancellationToken ct = default)
        {
            var semester = await _semesterRepository.GetActiveAsync(ct);
            return semester?.Id;
        }

        public async Task<int?> GetCurrentPhaseIdAsync(int semesterId, CancellationToken ct = default)
        {
            var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
            var currentPhase = semester?.Phases.FirstOrDefault(p =>
                p.StartDate <= _dateTimeService.UtcNow && p.EndDate >= _dateTimeService.UtcNow);
            return currentPhase?.Id;
        }

        public async Task<bool> IsWithinPhaseAsync(int semesterId, int phaseId, DateTime date, CancellationToken ct = default)
        {
            var semester = await _semesterRepository.GetWithPhasesAsync(semesterId, ct);
            var phase = semester?.Phases.FirstOrDefault(p => p.Id == phaseId);
            if (phase is null) return false;

            return date >= phase.StartDate && date <= phase.EndDate;
        }

        public async Task<int?> GetSemesterAfterAsync(int semesterId, int count, CancellationToken ct = default)
        {
            if (count <= 0)
            {
                return semesterId;
            }

            var targetSemester = await _semesterRepository.GetSemesterAfterAsync(semesterId, count, ct);
            return targetSemester?.Id;
        }
    }
}
