using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.ValueObjects
{
    public sealed class TopicCode : ValueObject
    {
        public const int MaxLength = 20;
        public string Value { get; }

        private TopicCode(string value) => Value = value;

        public static TopicCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Topic code cannot be empty.", nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentException($"Topic code cannot exceed {MaxLength} characters.", nameof(value));
            return new TopicCode(value.ToUpperInvariant().Trim());
        }

        public static TopicCode Generate(int year, int sequence) => new($"TP-{year}-{sequence:D3}");

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
        public static implicit operator string(TopicCode code) => code.Value;
    }
}
