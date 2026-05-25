using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Rules
{
    /// <summary>
    /// Ensures a new/updated semester's date range does not overlap with any existing semester.
    /// Two ranges overlap when: newStart &lt; existingEnd AND newEnd &gt; existingStart.
    /// </summary>
    public class SemesterDatesMustNotOverlapRule : IBusinessRule
    {
        private readonly bool _hasOverlap;

        public SemesterDatesMustNotOverlapRule(bool hasOverlap)
        {
            _hasOverlap = hasOverlap;
        }

        public string Message => "Khoảng thời gian học kỳ bị trùng lặp với một học kỳ khác đã tồn tại.";
        public bool IsBroken() => _hasOverlap;
    }
}
