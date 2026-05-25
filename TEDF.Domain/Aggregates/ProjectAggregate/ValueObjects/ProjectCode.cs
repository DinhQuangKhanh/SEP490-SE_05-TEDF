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
        /// Generates a new project code based on year and sequence number.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="sequence">The sequence number.</param>
        /// <returns>A new ProjectCode instance.</returns>
        public static ProjectCode Generate(int year, int sequence)
        {
            return new ProjectCode($"PROJ-{year}-{sequence:D3}");
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(ProjectCode code) => code.Value;
    }
}
