using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.GroupAggregate.Rules
{
    public class GroupMustHaveLeaderRule : IBusinessRule
    {
        private readonly bool _hasLeader;
        public GroupMustHaveLeaderRule(bool hasLeader) => _hasLeader = hasLeader;
        public string Message => "Group must have a leader.";
        public bool IsBroken() => !_hasLeader;
    }
}
