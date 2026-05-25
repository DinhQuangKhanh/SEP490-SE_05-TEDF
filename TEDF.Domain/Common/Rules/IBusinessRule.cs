

namespace TEDF.Domain.Common.Rules
{
    public interface IBusinessRule
    {
        /// <summary>
        /// Gets the error message when the rule is broken.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the error code for the broken rule.
        /// </summary>
        string Code => GetType().Name;

        /// <summary>
        /// Checks if the business rule is broken.
        /// </summary>
        /// <returns>True if the rule is broken; otherwise, false.</returns>
        bool IsBroken();
    }
}
