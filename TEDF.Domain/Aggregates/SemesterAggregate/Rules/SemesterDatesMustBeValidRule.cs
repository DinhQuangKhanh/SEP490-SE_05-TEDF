using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Rules
{
    public class SemesterDatesMustBeValidRule : IBusinessRule
    {
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
        public SemesterDatesMustBeValidRule(DateTime startDate, DateTime endDate)
        {
            _startDate = startDate;
            _endDate = endDate;
        }
        public string Message => "Semester end date must be after start date.";
        public bool IsBroken() => _endDate <= _startDate;
    }
}
