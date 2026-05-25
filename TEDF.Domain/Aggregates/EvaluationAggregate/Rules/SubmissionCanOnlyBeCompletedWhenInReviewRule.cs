using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Evaluation;

namespace TEDF.Domain.Aggregates.EvaluationAggregate.Rules
{
    public class SubmissionCanOnlyBeCompletedWhenInReviewRule : IBusinessRule
    {
        private readonly SubmissionStatus _status;
        public SubmissionCanOnlyBeCompletedWhenInReviewRule(SubmissionStatus status) => _status = status;
        public string Message => "Submission can only be completed when in review.";
        public bool IsBroken() => _status != SubmissionStatus.InReview;
    }
}
