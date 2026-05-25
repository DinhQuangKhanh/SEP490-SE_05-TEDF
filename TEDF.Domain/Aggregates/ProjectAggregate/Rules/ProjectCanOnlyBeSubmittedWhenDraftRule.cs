using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    public class ProjectCanOnlyBeSubmittedWhenDraftRule : IBusinessRule
    {
        private readonly ProjectStatus _currentStatus;
        private readonly bool _allowNeedsModification;

        public ProjectCanOnlyBeSubmittedWhenDraftRule(ProjectStatus currentStatus, bool allowNeedsModification = false)
        {
            _currentStatus = currentStatus;
            _allowNeedsModification = allowNeedsModification;
        }

        public string Message => _allowNeedsModification
            ? "Project can only be submitted when in Draft or NeedsModification status."
            : "Project can only be submitted when in Draft status.";

        public bool IsBroken()
        {
            if (_currentStatus == ProjectStatus.Draft)
                return false;

            if (_allowNeedsModification && _currentStatus == ProjectStatus.NeedsModification)
                return false;

            return true;
        }
    }
}
