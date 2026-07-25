using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.EvaluationAggregate.Rules;
using TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate
{
    public class EvaluationSubmission : AggregateRoot<Guid>
    {
        public Guid ProjectId { get; private set; }
        public int PhaseId { get; private set; }
        public SubmissionNumber SubmissionNumber { get; private set; } = null!;
        public Guid SubmittedBy { get; private set; }
        public DateTime SubmittedAt { get; private set; }
        public Guid? AssignedEvaluatorId { get; private set; }
        public DateTime? AssignedAt { get; private set; }
        public Guid? AssignedBy { get; private set; }
        public SubmissionStatus Status { get; private set; }
        public EvaluationResult Result { get; private set; }
        public DateTime? EvaluatedAt { get; private set; }
        public ProjectSnapshot? Snapshot { get; private set; }
        public string? Notes { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private EvaluationSubmission() { }

        public static EvaluationSubmission Create(Guid projectId, int phaseId, int submissionNumber, Guid submittedBy,
            ProjectSnapshot? snapshot = null, string? notes = null)
        {
            var submission = new EvaluationSubmission
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                PhaseId = phaseId,
                SubmissionNumber = SubmissionNumber.Create(submissionNumber),
                SubmittedBy = submittedBy,
                SubmittedAt = DateTime.UtcNow,
                Status = SubmissionStatus.Pending,
                Result = EvaluationResult.Pending,
                Snapshot = snapshot,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };
            submission.RaiseDomainEvent(new SubmissionCreatedEvent(submission.Id, projectId, phaseId, submittedBy, submissionNumber));
            return submission;
        }

        public void AssignEvaluator(Guid evaluatorId, Guid assignedBy, Guid projectId)
        {
            if (AssignedEvaluatorId.HasValue)
                throw new BusinessRuleValidationException("Evaluator is already assigned. Use ReassignEvaluator instead.");

            AssignedEvaluatorId = evaluatorId;
            AssignedAt = DateTime.UtcNow;
            AssignedBy = assignedBy;
            ProjectId = projectId;
            Status = SubmissionStatus.InReview;
            RaiseDomainEvent(new EvaluatorAssignedEvent(Id, evaluatorId, assignedBy, projectId));
        }

        public void ReassignEvaluator(Guid newEvaluatorId, Guid reassignedBy, Guid projectId)
        {
            var previousEvaluatorId = AssignedEvaluatorId;
            AssignedEvaluatorId = newEvaluatorId;
            AssignedAt = DateTime.UtcNow;
            AssignedBy = reassignedBy;
            ProjectId = projectId;
            RaiseDomainEvent(new EvaluatorReassignedEvent(Id, previousEvaluatorId, newEvaluatorId, reassignedBy, projectId));
        }

        public void Complete(EvaluationResult result)
        {
            CheckRule(new SubmissionCanOnlyBeCompletedWhenInReviewRule(Status));
            Status = SubmissionStatus.Completed;
            Result = result;
            EvaluatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new EvaluationCompletedEvent(Id, ProjectId, AssignedEvaluatorId, result));
        }

        public void Cancel()
        {
            if (Status == SubmissionStatus.Completed)
                throw new BusinessRuleValidationException("Completed submissions cannot be cancelled.");

            Status = SubmissionStatus.Cancelled;
            RaiseDomainEvent(new EvaluationCancelledEvent(Id, ProjectId));
        }

        public void SetNotes(string? notes) => Notes = notes;
    }
}
