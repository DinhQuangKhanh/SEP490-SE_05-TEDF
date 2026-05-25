using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects
{
    public sealed class SubmissionNumber : ValueObject
    {
        public int Value { get; }

        private SubmissionNumber(int value) => Value = value;

        public static SubmissionNumber Create(int value)
        {
            if (value < 1)
                throw new ArgumentException("Submission number must be at least 1.", nameof(value));
            return new SubmissionNumber(value);
        }

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value.ToString();
        public static implicit operator int(SubmissionNumber number) => number.Value;
    }
}
