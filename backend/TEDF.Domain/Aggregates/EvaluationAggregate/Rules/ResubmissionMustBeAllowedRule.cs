using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    public class ResubmissionMustBeAllowedRule : IBusinessRule
    {
        private readonly bool _isResubmissionAllowed;
        public ResubmissionMustBeAllowedRule(bool isResubmissionAllowed) => _isResubmissionAllowed = isResubmissionAllowed;
        public string Message => "Resubmission is not allowed for this project.";
        public bool IsBroken() => !_isResubmissionAllowed;
    }
}
