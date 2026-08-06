using System.Text.RegularExpressions;
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

        /// <summary>
        /// Short form used as the prefix of a project code: "FALL2025" → "FA25",
        /// "SUMMER2026" → "SU26", "SPRING2026" → "SP26". A code that is already short
        /// ("FA26") passes through unchanged, and anything unrecognised is returned as-is
        /// so a project code is still produced rather than an exception.
        /// </summary>
        public string ShortValue
        {
            get
            {
                var match = Regex.Match(Value, @"^(FALL|SUMMER|SPRING|FA|SU|SP)[-_ ]?(\d{2}|\d{4})$");
                if (!match.Success) return Value;

                var season = match.Groups[1].Value[..2];
                var year = match.Groups[2].Value;
                return $"{season}{year[^2..]}";
            }
        }

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
        public static implicit operator string(SemesterCode code) => code.Value;
    }
}
