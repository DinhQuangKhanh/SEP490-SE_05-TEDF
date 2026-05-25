using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Specifications.Semesters
{
    public class ActiveSemesterSpec : BaseSpecification<Semester>
    {
        public ActiveSemesterSpec()
            : base(s => s.StartDate <= DateTime.UtcNow && s.EndDate >= DateTime.UtcNow)
        {
            AddInclude(s => s.Phases);
        }
    }
}
