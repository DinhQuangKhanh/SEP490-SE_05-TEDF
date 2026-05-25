using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    public class ProjectMustHaveGroupRule : IBusinessRule
    {
        private readonly Guid? _groupId;

        public ProjectMustHaveGroupRule(Guid? groupId)
        {
            _groupId = groupId;
        }

        public string Message => "Project must be assigned to a student group.";

        public bool IsBroken() => !_groupId.HasValue;
    }
}
