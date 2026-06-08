using TEDF.Domain.Aggregates.EvaluationAggregate.Rules;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Entities
{
    /// <summary>
    /// Entity representing an evaluator assignment to a project.
    /// Each project should have exactly 3 evaluators assigned by Admin.
    /// Evaluators review projects independently and the final result is based on majority vote (2/3).
    /// </summary>
    public class ProjectEvaluatorAssignment : Entity<Guid>
    {
        public Guid ProjectId { get; private set; }
        public Guid EvaluatorId { get; private set; }
        public int EvaluatorOrder { get; private set; } // 1, 2, or 3
        public DateTime AssignedAt { get; private set; }
        public Guid AssignedBy { get; private set; }
        public bool IsActive { get; private set; } = true;

        // Individual evaluation result
        public EvaluationResult? IndividualResult { get; private set; }
        public DateTime? EvaluatedAt { get; private set; }
        public string? Feedback { get; private set; }

        private ProjectEvaluatorAssignment() { }

        /// <summary>
        /// Creates a new evaluator assignment for a project.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="evaluatorId">The evaluator's user ID.</param>
        /// <param name="order">The evaluator order (1, 2, or 3).</param>
        /// <param name="assignedBy">The admin who made the assignment.</param>
        /// <param name="allProjectMentorIds">All mentor IDs for the project (including inactive) for conflict check.</param>
        /// <param name="currentActiveEvaluatorCount">Current number of active evaluators for the project.</param>
        /// <returns>A new ProjectEvaluatorAssignment entity.</returns>
        /// <exception cref="BusinessRuleValidationException">Thrown when business rules are violated.</exception>
        public static ProjectEvaluatorAssignment Create(
            Guid projectId,
            Guid evaluatorId,
            int order,
            Guid assignedBy,
            IReadOnlyCollection<Guid> allProjectMentorIds,
            int currentActiveEvaluatorCount)
        {
            BusinessRuleValidator.CheckRules(
                new EvaluatorCannotEvaluateOwnMentoredProjectRule(evaluatorId, allProjectMentorIds),
                new ProjectCannotExceedMaxEvaluatorsRule(currentActiveEvaluatorCount));

            if (order is < 1 or > 2)
                throw new ArgumentOutOfRangeException(nameof(order), "Evaluator order must be 1 or 2.");

            return new ProjectEvaluatorAssignment
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EvaluatorId = evaluatorId,
                EvaluatorOrder = order,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy,
                IsActive = true,
                IndividualResult = null,
                EvaluatedAt = null,
                Feedback = null
            };
        }

        /// <summary>
        /// Submits the evaluator's individual evaluation result.
        /// </summary>
        /// <param name="result">The evaluation result.</param>
        /// <param name="feedback">Optional feedback/comments.</param>
        public void SubmitEvaluation(EvaluationResult result, string? feedback = null)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Cannot submit evaluation for an inactive assignment.");
            }

            if (IndividualResult.HasValue && IndividualResult.Value != EvaluationResult.Pending)
            {
                throw new InvalidOperationException("Evaluation has already been submitted.");
            }

            if (result == EvaluationResult.Pending)
            {
                throw new ArgumentException("Cannot submit Pending as a result.", nameof(result));
            }

            IndividualResult = result;
            EvaluatedAt = DateTime.UtcNow;
            Feedback = feedback;
        }

        /// <summary>
        /// Updates the feedback without changing the result.
        /// </summary>
        /// <param name="feedback">The new feedback.</param>
        public void UpdateFeedback(string? feedback)
        {
            Feedback = feedback;
        }

        /// <summary>
        /// Resets the evaluation result so the evaluator can re-evaluate.
        /// Used when a project is resubmitted for evaluation.
        /// </summary>
        public void ResetEvaluation()
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot reset evaluation for an inactive assignment.");

            IndividualResult = null;
            EvaluatedAt = null;
            Feedback = null;
        }

        /// <summary>
        /// Deactivates this evaluator assignment.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// Reactivates this evaluator assignment.
        /// </summary>
        public void Reactivate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Checks if this evaluator has submitted their evaluation.
        /// </summary>
        public bool HasSubmittedEvaluation =>
            IndividualResult.HasValue && IndividualResult.Value != EvaluationResult.Pending;
    }
}
