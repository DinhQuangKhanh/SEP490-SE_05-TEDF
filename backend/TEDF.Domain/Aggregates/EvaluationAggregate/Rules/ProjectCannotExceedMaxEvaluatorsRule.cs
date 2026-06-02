using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    /// <summary>
    /// Business rule that ensures a project cannot have more than 3 evaluators.
    /// </summary>
    public class ProjectCannotExceedMaxEvaluatorsRule : IBusinessRule
    {
        public const int MaxEvaluators = 2;
        private readonly int _currentEvaluatorCount;

        /// <summary>
        /// Creates a new instance of the rule.
        /// </summary>
        /// <param name="currentEvaluatorCount">The current number of active evaluators for the project.</param>
        public ProjectCannotExceedMaxEvaluatorsRule(int currentEvaluatorCount)
        {
            _currentEvaluatorCount = currentEvaluatorCount;
        }

        public string Message => $"Project cannot have more than {MaxEvaluators} evaluators.";

        public bool IsBroken() => _currentEvaluatorCount >= MaxEvaluators;
    }
}
