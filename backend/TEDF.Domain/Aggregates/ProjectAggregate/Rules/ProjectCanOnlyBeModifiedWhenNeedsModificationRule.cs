using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    public class ProjectCanOnlyBeModifiedWhenNeedsModificationRule : IBusinessRule
    {
        private readonly ProjectStatus _currentStatus;

        public ProjectCanOnlyBeModifiedWhenNeedsModificationRule(ProjectStatus currentStatus)
        {
            _currentStatus = currentStatus;
        }

        public string Message => "Project can only be modified when in NeedsModification status.";

        public bool IsBroken() => _currentStatus != ProjectStatus.NeedsModification;
    }
}
