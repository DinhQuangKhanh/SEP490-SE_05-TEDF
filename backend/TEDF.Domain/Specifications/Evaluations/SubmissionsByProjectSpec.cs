using TEDF.Domain.Aggregates.EvaluationAggregate;

namespace TEDF.Domain.Specifications.Evaluations
{
    public class SubmissionsByProjectSpec : BaseSpecification<EvaluationSubmission>
    {
        public SubmissionsByProjectSpec(Guid projectId) : base(s => s.ProjectId == projectId)
        {
            ApplyOrderBy(s => s.SubmissionNumber);
        }
    }
}
