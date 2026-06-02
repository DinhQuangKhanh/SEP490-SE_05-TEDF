using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Events
{
    public sealed record EvaluationCompletedEvent(Guid SubmissionId, Guid ProjectId, Guid? EvaluatorId, EvaluationResult Result) : DomainEventBase;
}
