using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Entities
{
    public class SemesterPhase : Entity<int>
    {
        public int SemesterId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public SemesterPhaseType Type { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public int Order { get; private set; }
        public SemesterPhaseStatus Status { get; private set; }

        public bool IsCurrent => Status == SemesterPhaseStatus.InProgress;
        public int DurationDays => (EndDate - StartDate).Days;

        private SemesterPhase() { }

        internal static SemesterPhase Create(int semesterId, string name, SemesterPhaseType type,
            DateTime startDate, DateTime endDate, int order)
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");

            return new SemesterPhase
            {
                SemesterId = semesterId,
                Name = name,
                Type = type,
                StartDate = startDate,
                EndDate = endDate,
                Order = order,
                Status = SemesterPhaseStatus.NotStarted
            };
        }

        public void Start()
        {
            if (Status != SemesterPhaseStatus.NotStarted)
                throw new BusinessRuleValidationException("Phase has already started.");
            Status = SemesterPhaseStatus.InProgress;
        }

        public void Complete()
        {
            if (Status != SemesterPhaseStatus.InProgress)
                throw new BusinessRuleValidationException("Phase is not in progress.");
            Status = SemesterPhaseStatus.Completed;
        }

        public void UpdateDates(DateTime startDate, DateTime endDate)
        {
            if (Status == SemesterPhaseStatus.Completed)
                throw new BusinessRuleValidationException("Cannot update dates of completed phase.");
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
