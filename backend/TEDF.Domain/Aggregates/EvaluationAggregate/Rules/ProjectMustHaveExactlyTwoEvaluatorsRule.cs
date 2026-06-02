using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    /// <summary>
    /// Business rule that ensures a project has exactly 2 evaluators before proceeding.
    /// Used when starting the evaluation process.
    /// </summary>
    public class ProjectMustHaveExactlyTwoEvaluatorsRule : IBusinessRule
    {
        public const int RequiredEvaluators = 2;
        private readonly int _currentEvaluatorCount;

        public ProjectMustHaveExactlyTwoEvaluatorsRule(int currentEvaluatorCount)
        {
            _currentEvaluatorCount = currentEvaluatorCount;
        }

        public string Message => $"Project must have exactly {RequiredEvaluators} evaluators assigned before evaluation can proceed.";

        public bool IsBroken() => _currentEvaluatorCount != RequiredEvaluators;
    }
}
