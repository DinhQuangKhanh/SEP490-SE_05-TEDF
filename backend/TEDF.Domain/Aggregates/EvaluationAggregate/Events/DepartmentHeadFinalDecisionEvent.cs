using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    /// <summary>
    /// Domain event raised when the Department Head makes a final decision
    /// on a project where the two evaluators gave conflicting results.
    /// </summary>
    /// <param name="Notes">
    /// The reason the Department Head gave for the decision, as typed on the review screen.
    /// Carried on the event because it is not persisted on the project.
    /// </param>
    public sealed record DepartmentHeadFinalDecisionEvent(
        Guid ProjectId,
        EvaluationResult FinalResult,
        Guid DecidedBy,
        string? Notes = null
    ) : DomainEventBase;
}
