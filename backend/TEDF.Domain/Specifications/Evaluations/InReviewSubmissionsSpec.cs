using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Specifications.Evaluations
{
    public class InReviewSubmissionsSpec : BaseSpecification<EvaluationSubmission>
    {
        public InReviewSubmissionsSpec(Guid? evaluatorId = null)
            : base(s => s.Status == SubmissionStatus.InReview &&
                        (!evaluatorId.HasValue || s.AssignedEvaluatorId == evaluatorId.Value))
        {
            ApplyOrderBy(s => s.AssignedAt!);
        }
    }
}
