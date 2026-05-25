using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    public class SubmissionMustHaveEvaluatorRule : IBusinessRule
    {
        private readonly Guid? _evaluatorId;
        public SubmissionMustHaveEvaluatorRule(Guid? evaluatorId) => _evaluatorId = evaluatorId;
        public string Message => "Submission must have an evaluator assigned.";
        public bool IsBroken() => !_evaluatorId.HasValue;
    }
}
