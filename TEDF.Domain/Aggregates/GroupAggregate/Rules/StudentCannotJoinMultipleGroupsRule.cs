using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.GroupAggregate.Rules
{
    public class StudentCannotJoinMultipleGroupsRule : IBusinessRule
    {
        private readonly bool _isAlreadyInGroup;
        public StudentCannotJoinMultipleGroupsRule(bool isAlreadyInGroup) => _isAlreadyInGroup = isAlreadyInGroup;
        public string Message => "Student is already a member of this group.";
        public bool IsBroken() => _isAlreadyInGroup;
    }
}
