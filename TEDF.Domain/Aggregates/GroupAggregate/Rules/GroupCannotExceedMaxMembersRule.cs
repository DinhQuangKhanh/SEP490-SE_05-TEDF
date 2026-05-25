using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.GroupAggregate.Rules
{
    public class GroupCannotExceedMaxMembersRule : IBusinessRule
    {
        private readonly int _currentCount;
        private readonly int _maxCount;
        public GroupCannotExceedMaxMembersRule(int currentCount, int maxCount)
        {
            _currentCount = currentCount;
            _maxCount = maxCount;
        }
        public string Message => $"Group cannot have more than {_maxCount} members.";
        public bool IsBroken() => _currentCount >= _maxCount;
    }
}
