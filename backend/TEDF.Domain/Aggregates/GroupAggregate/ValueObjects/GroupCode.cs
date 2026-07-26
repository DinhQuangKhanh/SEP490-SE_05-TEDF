using System.Text.RegularExpressions;
using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.GroupAggregate.ValueObjects
{
    /// <summary>
    /// Group code in the form <c>{SemesterCode}-SE_{NN}</c>, e.g. <c>SUMMER2026-SE_01</c>.
    /// The sequence is at least two digits and restarts at 01 in every semester.
    /// </summary>
    public sealed partial class GroupCode : ValueObject
    {
        /// <summary>Longest semester code (20) + "-SE_" (4) + a 4-digit sequence.</summary>
        public const int MaxLength = 30;

        /// <summary>Fixed discipline segment. Only Software Engineering groups exist today.</summary>
        public const string Discipline = "SE";

        // The semester segment may itself contain hyphens (e.g. FALL-2025); the greedy prefix plus
        // the anchored -SE_NN tail still splits it unambiguously.
        [GeneratedRegex(@"^(?<semester>[A-Z0-9][A-Z0-9-]*)-(?<name>SE_(?<seq>\d{2,}))$")]
        private static partial Regex FormatRegex();

        public string Value { get; }

        private GroupCode(string value) => Value = value;

        /// <summary>The semester-code prefix, e.g. <c>SUMMER2026</c>.</summary>
        public string SemesterPart => FormatRegex().Match(Value).Groups["semester"].Value;

        /// <summary>
        /// The part that doubles as the group name, e.g. <c>SE_01</c>. Group.Name is derived from
        /// this so the two can never drift apart.
        /// </summary>
        public string NamePart => FormatRegex().Match(Value).Groups["name"].Value;

        /// <summary>The numeric sequence within the semester, e.g. 1 for <c>SE_01</c>.</summary>
        public int Sequence => int.Parse(FormatRegex().Match(Value).Groups["seq"].Value);

        public static GroupCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Group code cannot be empty.", nameof(value));

            var normalized = value.Trim().ToUpperInvariant();

            if (normalized.Length > MaxLength)
                throw new ArgumentException($"Group code cannot exceed {MaxLength} characters.", nameof(value));

            if (!FormatRegex().IsMatch(normalized))
                throw new ArgumentException(
                    $"Group code '{value}' is invalid. Expected {{SemesterCode}}-SE_{{NN}} with at least " +
                    "two sequence digits, e.g. SUMMER2026-SE_01.", nameof(value));

            return new GroupCode(normalized);
        }

        /// <summary>
        /// Builds the code for the <paramref name="sequence"/>-th group of a semester.
        /// Sequences past 99 simply grow to three digits (SE_100), keeping the codes sortable.
        /// </summary>
        public static GroupCode Generate(string semesterCode, int sequence)
        {
            if (string.IsNullOrWhiteSpace(semesterCode))
                throw new ArgumentException("Semester code cannot be empty.", nameof(semesterCode));
            if (sequence < 1)
                throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "Sequence must be 1 or greater.");

            return Create($"{semesterCode.Trim().ToUpperInvariant()}-{Discipline}_{sequence:D2}");
        }

        /// <summary>Group name for a sequence, matching the <c>SE_NN</c> tail of the code.</summary>
        public static string BuildName(int sequence) => $"{Discipline}_{sequence:D2}";

        protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
        public override string ToString() => Value;
        public static implicit operator string(GroupCode code) => code.Value;
    }
}
