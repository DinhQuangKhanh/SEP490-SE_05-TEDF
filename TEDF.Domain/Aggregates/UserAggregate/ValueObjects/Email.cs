using TEDF.Domain.Aggregates.UserAggregate.Rules;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.UserAggregate.ValueObjects
{
    /// <summary>
    /// Value object representing a validated FPT email address.
    /// </summary>
    public sealed class Email : ValueObject
    {
        public const string AllowedDomain = "fpt.edu.vn";

        public string Value { get; }

        private Email(string value)
        {
            Value = value.ToLowerInvariant();
        }

        /// <summary>
        /// Creates a new Email value object with validation.
        /// </summary>
        /// <param name="value">The email address string.</param>
        /// <returns>A validated Email value object.</returns>
        /// <exception cref="BusinessRuleValidationException">Thrown when the email is not from @fpt.edu.vn domain.</exception>
        public static Email Create(string value)
        {
            BusinessRuleValidator.CheckRule(new EmailMustBeFptDomainRule(value));
            return new Email(value);
        }

        /// <summary>
        /// Gets the username part of the email (before @).
        /// </summary>
        public string Username => Value.Split('@')[0];

        /// <summary>
        /// Gets the domain part of the email (after @).
        /// </summary>
        public string Domain => Value.Split('@')[1];

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(Email email) => email.Value;
    }
}
