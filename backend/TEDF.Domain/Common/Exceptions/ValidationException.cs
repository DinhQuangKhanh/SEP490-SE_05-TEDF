namespace TEDF.Domain.Common.Exceptions
{
    public class ValidationException : DomainException
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public Dictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationException class.
        /// </summary>
        public ValidationException()
            : base("One or more validation failures have occurred.", "VALIDATION_ERROR")
        {
            Errors = new();
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ValidationException(string message)
            : base(message, "VALIDATION_ERROR")
        {
            Errors = new();
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class with errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation failures have occurred.", "VALIDATION_ERROR")
        {
            Errors = errors;
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class with a single field error.
        /// </summary>
        /// <param name="propertyName">The property name that failed validation.</param>
        /// <param name="errorMessage">The error message.</param>
        public ValidationException(string propertyName, string errorMessage)
            : base($"Validation failed for {propertyName}: {errorMessage}", "VALIDATION_ERROR")
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, [errorMessage] }
            };
        }

        /// <summary>
        /// Initializes a new instance of the ValidationException class with multiple errors for a field.
        /// </summary>
        /// <param name="propertyName">The property name that failed validation.</param>
        /// <param name="errorMessages">The error messages.</param>
        public ValidationException(string propertyName, IEnumerable<string> errorMessages)
            : base($"Validation failed for {propertyName}", "VALIDATION_ERROR")
        {
            Errors = new Dictionary<string, string[]>
            {
                { propertyName, [.. errorMessages] }
            };
        }

        /// <summary>
        /// Creates a ValidationException from FluentValidation failures.
        /// </summary>
        /// <param name="failures">The validation failures grouped by property.</param>
        /// <returns>A new ValidationException instance.</returns>
        public static ValidationException FromFailures(IEnumerable<KeyValuePair<string, IEnumerable<string>>> failures)
        {
            var errors = failures.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToArray());

            return new ValidationException(errors);
        }

        /// <summary>
        /// Gets a formatted string of all validation errors.
        /// </summary>
        /// <returns>A string containing all validation errors.</returns>
        public string GetErrorsString()
        {
            var errorMessages = Errors.SelectMany(
                kvp => kvp.Value.Select(v => $"{kvp.Key}: {v}"));

            return string.Join(Environment.NewLine, errorMessages);
        }
    }
}
