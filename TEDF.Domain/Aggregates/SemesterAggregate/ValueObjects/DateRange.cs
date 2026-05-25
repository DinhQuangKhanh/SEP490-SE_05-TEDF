using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects
{
    public sealed class DateRange : ValueObject
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public int DurationDays => (EndDate - StartDate).Days;

        private DateRange(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        public static DateRange Create(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
                throw new ArgumentException("End date must be after start date.");
            return new DateRange(startDate, endDate);
        }

        public bool Contains(DateTime date) => date >= StartDate && date <= EndDate;
        public bool Overlaps(DateRange other) => StartDate < other.EndDate && EndDate > other.StartDate;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return StartDate;
            yield return EndDate;
        }
    }
}
