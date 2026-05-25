using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    /// <summary>
    /// Validates that a project status transition is allowed based on the defined state machine.
    /// </summary>
    public class ProjectStatusTransitionRule : IBusinessRule
    {
        private static readonly Dictionary<ProjectStatus, ProjectStatus[]> ValidTransitions = new()
        {
            [ProjectStatus.Draft] = [ProjectStatus.PendingEvaluation, ProjectStatus.Cancelled],
            [ProjectStatus.PendingEvaluation] = [ProjectStatus.Approved, ProjectStatus.NeedsModification, ProjectStatus.Rejected, ProjectStatus.Cancelled],
            [ProjectStatus.NeedsModification] = [ProjectStatus.PendingEvaluation, ProjectStatus.Cancelled],
            [ProjectStatus.Approved] = [ProjectStatus.InProgress, ProjectStatus.Cancelled],
            [ProjectStatus.Rejected] = [ProjectStatus.Draft],
            [ProjectStatus.InProgress] = [ProjectStatus.Completed, ProjectStatus.Cancelled],
            [ProjectStatus.Completed] = [],
            [ProjectStatus.Cancelled] = []
        };

        private readonly ProjectStatus _currentStatus;
        private readonly ProjectStatus _targetStatus;

        public ProjectStatusTransitionRule(ProjectStatus currentStatus, ProjectStatus targetStatus)
        {
            _currentStatus = currentStatus;
            _targetStatus = targetStatus;
        }

        public string Message => $"Cannot transition from {_currentStatus} to {_targetStatus}.";

        public bool IsBroken()
        {
            if (!ValidTransitions.TryGetValue(_currentStatus, out var validTargets))
                return true;

            return !validTargets.Contains(_targetStatus);
        }
    }
}
