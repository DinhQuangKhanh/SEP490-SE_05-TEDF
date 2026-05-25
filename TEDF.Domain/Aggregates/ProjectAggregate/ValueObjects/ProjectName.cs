using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects
{
    public sealed class ProjectName : ValueObject
    {
        /// <summary>
        /// Maximum length of a project name.
        /// </summary>
        public const int MaxLength = 350;

        /// <summary>
        /// Gets the project name value.
        /// </summary>
        public string Value { get; }

        private ProjectName(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new project name from the specified value.
        /// </summary>
        /// <param name="value">The project name value.</param>
        /// <returns>A new ProjectName instance.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is invalid.</exception>
        public static ProjectName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project name cannot be empty.", nameof(value));

            if (value.Length > MaxLength)
                throw new ArgumentException($"Project name cannot exceed {MaxLength} characters.", nameof(value));

            return new ProjectName(value.Trim());
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(ProjectName name) => name.Value;
    }
}
