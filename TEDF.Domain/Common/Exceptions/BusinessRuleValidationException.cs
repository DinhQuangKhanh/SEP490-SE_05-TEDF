using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Common.Exceptions
{
    public class BusinessRuleValidationException : DomainException
    {
        /// <summary>
        /// Gets the broken business rule.
        /// </summary>
        public IBusinessRule? BrokenRule { get; }

        /// <summary>
        /// Gets the details of the validation failure.
        /// </summary>
        public string Details { get; }

        /// <summary>
        /// Initializes a new instance with a broken business rule.
        /// </summary>
        /// <param name="brokenRule">The broken business rule.</param>
        public BusinessRuleValidationException(IBusinessRule brokenRule)
            : base(brokenRule.Message, brokenRule.Code)
        {
            BrokenRule = brokenRule;
            Details = brokenRule.Message;
        }

        /// <summary>
        /// Initializes a new instance with a message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BusinessRuleValidationException(string message)
            : base(message, "BUSINESS_RULE_VIOLATION")
        {
            Details = message;
        }

        /// <summary>
        /// Initializes a new instance with a message and error code.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="code">The error code.</param>
        public BusinessRuleValidationException(string message, string code)
            : base(message, code)
        {
            Details = message;
        }
    }
}
