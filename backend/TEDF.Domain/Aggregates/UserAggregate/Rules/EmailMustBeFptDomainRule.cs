using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.UserAggregate.Rules
{
    /// <summary>
    /// Business rule that validates an account email belongs to an accepted domain.
    /// </summary>
    /// <remarks>
    /// The class name is kept (rather than renamed to match the widened rule) because
    /// <c>IBusinessRule.Code</c> defaults to the class name and clients already handle
    /// <c>EmailMustBeFptDomainRule</c> as the error code for a rejected address.
    /// </remarks>
    public class EmailMustBeFptDomainRule : IBusinessRule
    {
        /// <summary>
        /// Domains an account address may belong to: students use @fpt.edu.vn, lecturers
        /// @fe.edu.vn, and @gmail.com is accepted so test accounts can be created without a
        /// school mailbox. This is validated on every read of the address, not just on
        /// creation, so removing a domain here orphans any row already stored under it.
        /// </summary>
        public static readonly string[] AllowedDomains = ["fpt.edu.vn", "fe.edu.vn", "gmail.com"];

        private readonly string _email;

        public EmailMustBeFptDomainRule(string email)
        {
            _email = email;
        }

        public string Message =>
            $"Email must be from one of these domains: {string.Join(", ", AllowedDomains.Select(d => $"@{d}"))}.";

        public bool IsBroken()
        {
            if (string.IsNullOrWhiteSpace(_email))
                return true;

            return !AllowedDomains.Any(domain => _email.EndsWith($"@{domain}", StringComparison.OrdinalIgnoreCase));
        }
    }
}
