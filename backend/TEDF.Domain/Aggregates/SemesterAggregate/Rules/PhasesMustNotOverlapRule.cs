using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Rules
{
    public class PhasesMustNotOverlapRule : IBusinessRule
    {
        private readonly IEnumerable<(DateTime Start, DateTime End)> _existingPhases;
        private readonly DateTime _newStart;
        private readonly DateTime _newEnd;

        public PhasesMustNotOverlapRule(IEnumerable<(DateTime Start, DateTime End)> existingPhases, DateTime newStart, DateTime newEnd)
        {
            _existingPhases = existingPhases;
            _newStart = newStart;
            _newEnd = newEnd;
        }

        public string Message => "Phase dates overlap with existing phases.";
        public bool IsBroken() => _existingPhases.Any(p => _newStart < p.End && _newEnd > p.Start);
    }
}
