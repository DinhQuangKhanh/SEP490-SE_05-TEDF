using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    public class MaxResubmissionsNotExceededRule : IBusinessRule
    {
        private readonly int _currentCount;
        private readonly int _maxCount;
        public MaxResubmissionsNotExceededRule(int currentCount, int maxCount)
        {
            _currentCount = currentCount;
            _maxCount = maxCount;
        }
        public string Message => $"Maximum resubmissions ({_maxCount}) exceeded.";
        public bool IsBroken() => _currentCount >= _maxCount;
    }
}
