using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects
{
    public sealed class AcademicYear : ValueObject
    {
        public int StartYear { get; }
        public int EndYear { get; }
        public string Value { get; }

        private AcademicYear(int startYear, int endYear)
        {
            StartYear = startYear;
            EndYear = endYear;
            Value = $"{startYear}-{endYear}";
        }

        public static AcademicYear Create(int startYear)
        {
            if (startYear < 2000 || startYear > 2100)
                throw new ArgumentException("Invalid year.", nameof(startYear));
            return new AcademicYear(startYear, startYear + 1);
        }

        public static AcademicYear Parse(string value)
        {
            var parts = value.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var startYear))
                throw new ArgumentException("Invalid academic year format.", nameof(value));
            return new AcademicYear(startYear, startYear + 1);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return StartYear;
            yield return EndYear;
        }

        public override string ToString() => Value;
        public static implicit operator string(AcademicYear year) => year.Value;
    }
}
