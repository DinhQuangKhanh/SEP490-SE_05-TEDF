using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.SupportAggregate.ValueObjects
{
    public sealed class TicketCode : ValueObject
    {
        public const int MaxLength = 20;
        public string Value { get; }

        private TicketCode(string value) => Value = value;

        public static TicketCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Ticket code cannot be empty.", nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentException($"Ticket code cannot exceed {MaxLength} characters.", nameof(value));
            return new TicketCode(value.ToUpperInvariant().Trim());
        }

        public static TicketCode Generate(int year, int sequence) => new($"TK-{year}-{sequence:D4}");

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
        public static implicit operator string(TicketCode code) => code.Value;
    }
}
