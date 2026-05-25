using TEDF.Domain.Common.Exceptions;

namespace TEDF.Domain.Common.Rules
{
    public static class BusinessRuleValidator
    {
        /// <summary>
        /// Checks if a business rule is satisfied. Throws an exception if the rule is broken.
        /// </summary>
        /// <param name="rule">The business rule to check.</param>
        /// <exception cref="BusinessRuleValidationException">Thrown when the rule is broken.</exception>
        public static void CheckRule(IBusinessRule rule)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleValidationException(rule);
            }
        }

        /// <summary>
        /// Checks multiple business rules. Throws an exception for the first broken rule.
        /// </summary>
        /// <param name="rules">The business rules to check.</param>
        /// <exception cref="BusinessRuleValidationException">Thrown when any rule is broken.</exception>
        public static void CheckRules(params IBusinessRule[] rules)
        {
            foreach (var rule in rules)
            {
                CheckRule(rule);
            }
        }

        /// <summary>
        /// Validates all rules and returns a list of broken rules.
        /// </summary>
        /// <param name="rules">The business rules to validate.</param>
        /// <returns>A list of broken rules.</returns>
        public static IReadOnlyList<IBusinessRule> ValidateRules(params IBusinessRule[] rules)
        {
            return rules.Where(r => r.IsBroken()).ToList();
        }
    }
}
