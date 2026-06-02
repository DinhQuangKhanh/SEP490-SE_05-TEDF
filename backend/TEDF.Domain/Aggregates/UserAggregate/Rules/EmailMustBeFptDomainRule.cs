using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.UserAggregate.Rules
{
    /// <summary>
    /// Business rule that validates email must be from @fpt.edu.vn domain.
    /// </summary>
    public class EmailMustBeFptDomainRule : IBusinessRule
    {
        private const string AllowedDomain = "@fpt.edu.vn";
        private readonly string _email;

        public EmailMustBeFptDomainRule(string email)
        {
            _email = email;
        }

        public string Message => $"Email must be from {AllowedDomain} domain.";

        public bool IsBroken()
        {
            if (string.IsNullOrWhiteSpace(_email))
                return true;

            return !_email.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase);
        }
    }
}
