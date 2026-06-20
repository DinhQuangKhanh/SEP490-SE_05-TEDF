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
        private readonly List<EligibleMentor> _eligibleMentors = [];

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

        /// <summary>When the eligibility roster was finalized &amp; notifications/emails dispatched. Null until published.</summary>
        public DateTime? RosterPublishedAt { get; private set; }

        public IReadOnlyCollection<SemesterPhase> Phases => _phases.AsReadOnly();
        public IReadOnlyCollection<EligibleStudent> EligibleStudents => _eligibleStudents.AsReadOnly();
        public IReadOnlyCollection<EligibleMentor> EligibleMentors => _eligibleMentors.AsReadOnly();
        public SemesterPhase? CurrentPhase => _phases.FirstOrDefault(p => p.IsCurrent);
        public bool IsActive => Status == SemesterStatus.Ongoing;
        public bool IsRosterPublished => RosterPublishedAt.HasValue;

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

        public void AddEligibleStudent(Guid studentId, string studentCode,
            string? email = null, string? phoneNumber = null, int? majorId = null, Guid? importedBy = null)
        {
            var existing = _eligibleStudents.FirstOrDefault(s => s.StudentId == studentId);
            if (existing != null)
            {
                // Supplementary import: ignore the match but refresh the snapshot and reinstate if revoked.
                existing.UpdateSnapshot(email, phoneNumber, majorId);
                if (!existing.IsEligible)
                    existing.ReinstateEligibility();
            }
            else
            {
                _eligibleStudents.Add(
                    EligibleStudent.Create(Id, studentId, studentCode, email, phoneNumber, majorId, importedBy));
            }
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds (or reinstates) a mentor assigned to supervise in this semester. Idempotent: a mentor
        /// already on the roster is left in place (snapshot/major refreshed) — implementing the
        /// "ignore matches, add the rest" supplementary-import rule.
        /// </summary>
        public void AddEligibleMentor(Guid mentorId, string employeeCode, int? majorId,
            string? email = null, string? phoneNumber = null, string? division = null, Guid? importedBy = null)
        {
            var existing = _eligibleMentors.FirstOrDefault(m => m.MentorId == mentorId);
            if (existing != null)
            {
                existing.UpdateSnapshot(email, phoneNumber, division);
                // Chỉ đổi ngành khi import cung cấp giá trị — tránh xóa ngành admin đã chọn.
                if (majorId.HasValue)
                    existing.ChangeMajor(majorId.Value);
                if (!existing.IsAssigned)
                    existing.Reinstate();
            }
            else
            {
                _eligibleMentors.Add(
                    EligibleMentor.Create(Id, mentorId, employeeCode, majorId, email, phoneNumber, division, importedBy));
            }
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Corrects the assigned program of a rostered mentor (the inline "Program" edit).</summary>
        public void UpdateEligibleMentorMajor(Guid mentorId, int majorId)
        {
            var mentor = _eligibleMentors.FirstOrDefault(m => m.MentorId == mentorId)
                ?? throw new EntityNotFoundException(nameof(EligibleMentor), mentorId);
            mentor.ChangeMajor(majorId);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Finalizes the eligibility roster and raises <see cref="SemesterRosterPublishedEvent"/> so handlers
        /// notify mentors and email students. Only allowed while the semester is still Upcoming.
        /// </summary>
        public void PublishRoster(Guid publishedBy)
        {
            EnsureUpcoming();

            // Mọi giảng viên được phân công phải có Ngành (để thông báo kèm tên ngành).
            var missingMajor = _eligibleMentors
                .Where(m => m.IsAssigned && m.MajorId is null)
                .Select(m => m.EmployeeCode)
                .ToList();
            if (missingMajor.Count > 0)
                throw new BusinessRuleValidationException(
                    "Vui lòng chọn Ngành cho các giảng viên trước khi công bố: " + string.Join(", ", missingMajor) + ".");

            RosterPublishedAt = DateTime.UtcNow;
            UpdatedAt = RosterPublishedAt;
            RaiseDomainEvent(new SemesterRosterPublishedEvent(Id, publishedBy));
        }

        private void EnsureUpcoming()
        {
            if (Status != SemesterStatus.Upcoming)
                throw new BusinessRuleValidationException(
                    "Chỉ có thể chỉnh sửa học kỳ khi chưa bắt đầu (trạng thái Sắp tới).");
        }
    }
}
