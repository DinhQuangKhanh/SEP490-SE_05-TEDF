using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    /// <summary>
    /// Business rule that ensures a project has exactly 3 evaluators before proceeding.
    /// Used when starting the evaluation process.
    /// </summary>
    public class ProjectMustHaveExactlyThreeEvaluatorsRule : IBusinessRule
    {
        public const int RequiredEvaluators = 3;
        private readonly int _currentEvaluatorCount;

        /// <summary>
        /// Creates a new instance of the rule.
        /// </summary>
        /// <param name="currentEvaluatorCount">The current number of active evaluators for the project.</param>
        public ProjectMustHaveExactlyThreeEvaluatorsRule(int currentEvaluatorCount)
        {
            _currentEvaluatorCount = currentEvaluatorCount;
        }

        public string Message => $"Project must have exactly {RequiredEvaluators} evaluators assigned before evaluation can proceed.";

        public bool IsBroken() => _currentEvaluatorCount != RequiredEvaluators;
    }
}
