using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.GroupAggregate.Rules
{
    public class GroupMustHaveMinMembersRule : IBusinessRule
    {
        private readonly int _currentCount;
        private readonly int _minCount;

        public GroupMustHaveMinMembersRule(int currentCount, int minCount)
        {
            _currentCount = currentCount;
            _minCount = minCount;
        }

        public string Message => $"Group must have at least {_minCount} members. Current: {_currentCount}.";
        public bool IsBroken() => _currentCount < _minCount;
    }
}
