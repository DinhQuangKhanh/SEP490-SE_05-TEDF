using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    /// <summary>
    /// Business rule that prevents an evaluator from evaluating a project they have mentored.
    /// This rule checks ALL mentors (including inactive ones) to prevent any conflict of interest.
    /// </summary>
    public class EvaluatorCannotEvaluateOwnMentoredProjectRule : IBusinessRule
    {
        private readonly Guid _evaluatorId;
        private readonly IReadOnlyCollection<Guid> _allProjectMentorIds;

        /// <summary>
        /// Creates a new instance of the rule.
        /// </summary>
        /// <param name="evaluatorId">The ID of the evaluator being assigned.</param>
        /// <param name="allProjectMentorIds">All mentor IDs for the project (including inactive mentors).</param>
        public EvaluatorCannotEvaluateOwnMentoredProjectRule(
            Guid evaluatorId,
            IReadOnlyCollection<Guid> allProjectMentorIds)
        {
            _evaluatorId = evaluatorId;
            _allProjectMentorIds = allProjectMentorIds ?? throw new ArgumentNullException(nameof(allProjectMentorIds));
        }

        public string Message => "Evaluator cannot evaluate a project they have mentored. Conflict of interest.";

        public bool IsBroken() => _allProjectMentorIds.Contains(_evaluatorId);
    }
}
