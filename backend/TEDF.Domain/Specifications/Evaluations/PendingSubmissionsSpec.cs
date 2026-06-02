using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Specifications.Evaluations
{
    public class PendingSubmissionsSpec : BaseSpecification<EvaluationSubmission>
    {
        public PendingSubmissionsSpec()
            : base(s => s.Status == SubmissionStatus.Pending)
        {
            ApplyOrderBy(s => s.SubmittedAt);
        }
    }
}
