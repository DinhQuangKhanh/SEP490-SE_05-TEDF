using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.GroupAggregate.ValueObjects
{
    public sealed class GroupCode : ValueObject
    {
        public const int MaxLength = 20;
        public string Value { get; }

        private GroupCode(string value) => Value = value;

        public static GroupCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Group code cannot be empty.", nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentException($"Group code cannot exceed {MaxLength} characters.", nameof(value));
            return new GroupCode(value.ToUpperInvariant().Trim());
        }

        public static GroupCode Generate(int year, int sequence) => new($"G-{year}-{sequence:D3}");

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
        public static implicit operator string(GroupCode code) => code.Value;
    }
}
