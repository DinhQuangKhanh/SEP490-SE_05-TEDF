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
        /// <summary>Domains an address may belong to; see <see cref="EmailMustBeFptDomainRule"/>.</summary>
        public static IReadOnlyList<string> AllowedDomains => EmailMustBeFptDomainRule.AllowedDomains;

        /// <summary>
        /// Whether <paramref name="value"/> would be accepted by <see cref="Create"/>, without
        /// throwing. Use this where a rejected address is an expected outcome rather than an error.
        /// </summary>
        public static bool IsAllowed(string? value) => !new EmailMustBeFptDomainRule(value ?? string.Empty).IsBroken();

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
