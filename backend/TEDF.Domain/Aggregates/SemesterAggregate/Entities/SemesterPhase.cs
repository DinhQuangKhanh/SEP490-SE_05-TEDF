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

        /// <summary>
        /// Lifecycle status of the phase, derived from <see cref="StartDate"/>, <see cref="EndDate"/>
        /// and the current time. This is a <b>database computed column</b> (see
        /// SemesterPhaseConfiguration): SQL Server evaluates it on read as
        /// NotStarted (now &lt; StartDate), InProgress (StartDate ≤ now ≤ EndDate) or
        /// Completed (now &gt; EndDate). EF Core never inserts/updates this value — the
        /// private setter only exists so EF can materialize the computed value when reading.
        /// </summary>
        public SemesterPhaseStatus Status { get; private set; }

        public bool IsCurrent => Status == SemesterPhaseStatus.InProgress;
        public int DurationDays => (EndDate - StartDate).Days;

        private SemesterPhase() { }

        internal static SemesterPhase Create(int semesterId, string name, SemesterPhaseType type,
            DateTime startDate, DateTime endDate, int order)
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");

            // Status is intentionally not set here: it is a database computed column
            // derived from StartDate/EndDate and the current time (see SemesterPhaseConfiguration).
            return new SemesterPhase
            {
                SemesterId = semesterId,
                Name = name,
                Type = type,
                StartDate = startDate,
                EndDate = endDate,
                Order = order
            };
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
