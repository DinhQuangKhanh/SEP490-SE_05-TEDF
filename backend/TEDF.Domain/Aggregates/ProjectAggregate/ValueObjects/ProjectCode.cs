using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects
{
    public sealed class ProjectCode : ValueObject
    {
        /// <summary>
        /// Maximum length of a project code.
        /// </summary>
        public const int MaxLength = 20;

        /// <summary>
        /// Gets the project code value.
        /// </summary>
        public string Value { get; }

        private ProjectCode(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new project code from the specified value.
        /// </summary>
        /// <param name="value">The project code value.</param>
        /// <returns>A new ProjectCode instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
        public static ProjectCode Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project code cannot be empty.", nameof(value));

            if (value.Length > MaxLength)
                throw new ArgumentException($"Project code cannot exceed {MaxLength} characters.", nameof(value));

            return new ProjectCode(value.ToUpperInvariant().Trim());
        }

        /// <summary>
        /// Builds the prefix shared by every project of one semester and major, e.g. "FA26-SE-".
        /// Used both to generate a code and to find the highest sequence already taken.
        /// </summary>
        /// <param name="semesterShortCode">Short semester code, e.g. "FA26" (see SemesterCode.ShortValue).</param>
        /// <param name="majorCode">Major code, e.g. "SE".</param>
        public static string BuildPrefix(string semesterShortCode, string majorCode)
        {
            return $"{semesterShortCode.Trim().ToUpperInvariant()}-{majorCode.Trim().ToUpperInvariant()}-";
        }

        /// <summary>
        /// Generates a project code of the form &lt;semester&gt;-&lt;major&gt;-&lt;sequence&gt;,
        /// e.g. "FA26-SE-01". The sequence is padded to two digits and grows past it naturally
        /// once a semester/major passes 99 topics.
        /// </summary>
        public static ProjectCode Generate(string semesterShortCode, string majorCode, int sequence)
        {
            return new ProjectCode($"{BuildPrefix(semesterShortCode, majorCode)}{sequence:D2}");
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(ProjectCode code) => code.Value;
    }
}
