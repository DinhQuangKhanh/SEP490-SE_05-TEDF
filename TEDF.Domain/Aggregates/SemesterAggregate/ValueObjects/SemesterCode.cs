using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SemesterAggregate.ValueObjects
{
    public sealed class SemesterCode : ValueObject
    {
        public const int MaxLength = 20;
        public string Value { get; }

        private SemesterCode(string value) => Value = value;

        public static SemesterCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Semester code cannot be empty.", nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentException($"Semester code cannot exceed {MaxLength} characters.", nameof(value));
            return new SemesterCode(value.ToUpperInvariant().Trim());
        }

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
        public static implicit operator string(SemesterCode code) => code.Value;
    }
}
