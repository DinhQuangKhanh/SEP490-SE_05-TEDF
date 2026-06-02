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
            if (endDate < startDate)
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

            EnsurePhaseWithinSemester(type, startDate, endDate);

            var order = _phases.Count + 1;
            var phase = SemesterPhase.Create(Id, name, type, startDate, endDate, order);
            _phases.Add(phase);
            UpdatedAt = DateTime.UtcNow;
            return phase;
        }

        /// <summary>
        /// Validates a phase's dates relative to THIS semester, by phase type:
        /// Registration and Evaluation happen during the previous (current) semester, so they must
        /// finish on or before this semester starts; Implementation and Defense must fall within it.
        /// The "within the current semester" lower bound for Registration/Evaluation is enforced by
        /// the application handlers (which can load the active semester).
        /// </summary>
        private void EnsurePhaseWithinSemester(SemesterPhaseType type, DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
                throw new BusinessRuleValidationException("Ngày kết thúc giai đoạn phải sau ngày bắt đầu.");

            if (type is SemesterPhaseType.Registration or SemesterPhaseType.Evaluation)
            {
                if (endDate > StartDate)
                    throw new BusinessRuleValidationException(
                        "Giai đoạn Đăng ký và Thẩm định phải kết thúc trước khi kỳ học mới bắt đầu.");
            }
            else // Implementation, Defense
            {
                if (startDate < StartDate || endDate > EndDate)
                    throw new BusinessRuleValidationException(
                        "Giai đoạn Thực hiện và Bảo vệ phải nằm trong thời gian của kỳ học.");
            }
        }

        public void StartPhase(int phaseId)
        {
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);

            if (CurrentPhase != null)
                throw new BusinessRuleValidationException("Complete current phase before starting a new one.");

            // Phase status is a time-derived computed column; this method only raises the
            // notification event (the transition itself happens automatically by date).
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new PhaseStartedEvent(Id, phaseId, phase.Type));
        }

        public void CompletePhase(int phaseId)
        {
            var phase = _phases.FirstOrDefault(p => p.Id == phaseId)
                ?? throw new EntityNotFoundException(nameof(SemesterPhase), phaseId);

            // Phase status is a time-derived computed column; this method only raises the
            // notification event (the transition itself happens automatically by date).
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

            // Type-aware bounds relative to this semester (same rule as AddPhase). Overlap is not
            // re-checked per phase here: updates are applied one at a time, so an intermediate state
            // could transiently overlap a not-yet-updated phase even when the final set is valid —
            // the client validates non-overlap across the whole submitted set before sending.
            EnsurePhaseWithinSemester(phase.Type, startDate, endDate);

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
