using TEDF.Domain.Aggregates.SemesterAggregate.Entities;
using TEDF.Domain.Aggregates.SemesterAggregate.Events;
using TEDF.Domain.Aggregates.SemesterAggregate.Rules;
using TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Aggregates.SemesterAggregate
{
    public class Semester : AggregateRoot<int>
    {
        private readonly List<SemesterPhase> _phases = [];
        private readonly List<EligibleStudent> _eligibleStudents = [];

        public string Name { get; private set; } = string.Empty;
        public SemesterCode Code { get; private set; } = null!;
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public SemesterStatus Status
        {
            get
            {
                var now = DateTime.UtcNow;
                if (now < StartDate) return SemesterStatus.Upcoming;
                if (now > EndDate) return SemesterStatus.Ended;
                return SemesterStatus.Ongoing;
            }
        }
        public AcademicYear AcademicYear { get; private set; } = null!;
        public string? Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public IReadOnlyCollection<SemesterPhase> Phases => _phases.AsReadOnly();
        public IReadOnlyCollection<EligibleStudent> EligibleStudents => _eligibleStudents.AsReadOnly();
        public SemesterPhase? CurrentPhase => _phases.FirstOrDefault(p => p.IsCurrent);
        public bool IsActive => Status == SemesterStatus.Ongoing;

        private Semester() { }

        public static Semester Create(int id, string name, SemesterCode code, DateTime startDate, DateTime endDate,
            AcademicYear academicYear, string? description = null)
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");

            var semester = new Semester
            {
                Id = id,
                Name = name,
                Code = code,
                StartDate = startDate,
                EndDate = endDate,
                AcademicYear = academicYear,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            semester.RaiseDomainEvent(new SemesterCreatedEvent(id, code.Value));
            return semester;
        }

        public SemesterPhase AddPhase(string name, SemesterPhaseType type, DateTime startDate, DateTime endDate)
        {
            var existingPhases = _phases.Select(p => (p.StartDate, p.EndDate));
            CheckRule(new PhasesMustNotOverlapRule(existingPhases, startDate, endDate));

            if (startDate < StartDate || endDate > EndDate)
                throw new BusinessRuleValidationException("Phase dates must be within semester dates.");

            var order = _phases.Count + 1;
            var phase = SemesterPhase.Create(Id, name, type, startDate, endDate, order);
            _phases.Add(phase);
            UpdatedAt = DateTime.UtcNow;
            return phase;
        }

        public void StartPhase(int phaseId)
        {
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);

            if (CurrentPhase != null)
                throw new BusinessRuleValidationException("Complete current phase before starting a new one.");

            phase.Start();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new PhaseStartedEvent(Id, phaseId, phase.Type));
        }

        public void CompletePhase(int phaseId)
        {
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);
            phase.Complete();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new PhaseCompletedEvent(Id, phaseId, phase.Type));
        }

        public void NotifyUpcomingPhase(int phaseId)
        {
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);
                
            RaiseDomainEvent(new PhaseUpcomingEvent(Id, phaseId, phase.Type));
        }


        public void UpdateDates(DateTime startDate, DateTime endDate)
        {
            EnsureUpcoming();
            CheckRule(new SemesterDatesMustBeValidRule(startDate, endDate));
            StartDate = startDate;
            EndDate = endDate;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDetails(string name, string? description)
        {
            EnsureUpcoming();
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleValidationException("Semester name cannot be empty.");

            Name = name;
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePhaseDates(int phaseId, DateTime startDate, DateTime endDate)
        {
            EnsureUpcoming();
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);
            phase.UpdateDates(startDate, endDate);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddEligibleStudent(Guid studentId, string studentCode, Guid? importedBy = null)
        {
            if (_eligibleStudents.Any(s => s.StudentId == studentId && s.IsEligible))
                return;

            var existing = _eligibleStudents.FirstOrDefault(s => s.StudentId == studentId);
            if (existing != null)
            {
                existing.ReinstateEligibility();
            }
            else
            {
                var eligibleStudent = EligibleStudent.Create(Id, studentId, studentCode, importedBy);
                _eligibleStudents.Add(eligibleStudent);
            }
            UpdatedAt = DateTime.UtcNow;
        }

        private void EnsureUpcoming()
        {
            if (Status != SemesterStatus.Upcoming)
                throw new BusinessRuleValidationException(
                    "Chỉ có thể chỉnh sửa học kỳ khi chưa bắt đầu (trạng thái Sắp tới).");
        }
    }
}
