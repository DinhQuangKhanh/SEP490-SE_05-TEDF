using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects
{
    public sealed class TechnologyStack : ValueObject
    {
        /// <summary>
        /// Maximum length of the technology stack string. Widened to 4000 so a full parsed tech list
        /// (Front-end/Back-end/… gathered from the register form) fits.
        /// </summary>
        public const int MaxLength = 4000;

        /// <summary>
        /// Gets the technology stack value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the individual technologies as a list.
        /// </summary>
        public IReadOnlyList<string> Technologies { get; }

        private TechnologyStack(string value, IReadOnlyList<string> technologies)
        {
            Value = value;
            Technologies = technologies;
        }

        /// <summary>
        /// Creates a new technology stack from the specified value.
        /// </summary>
        /// <param name="value">The technology stack as a comma-separated string.</param>
        /// <returns>A new TechnologyStack instance.</returns>
        public static TechnologyStack Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new TechnologyStack(string.Empty, []);

            if (value.Length > MaxLength)
                throw new ArgumentException($"Technology stack cannot exceed {MaxLength} characters.", nameof(value));

            var technologies = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            return new TechnologyStack(value.Trim(), technologies);
        }

        /// <summary>
        /// Creates a new technology stack from a list of technologies.
        /// </summary>
        /// <param name="technologies">The list of technologies.</param>
        /// <returns>A new TechnologyStack instance.</returns>
        public static TechnologyStack FromList(IEnumerable<string> technologies)
        {
            var techList = technologies.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            var value = string.Join(", ", techList);

            if (value.Length > MaxLength)
                throw new ArgumentException($"Technology stack cannot exceed {MaxLength} characters.");

            return new TechnologyStack(value, techList);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(TechnologyStack stack) => stack.Value;
    }
}
