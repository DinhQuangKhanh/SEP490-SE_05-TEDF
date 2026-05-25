using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Enums.Semester;

namespace TEDF.Domain.Specifications.Semesters
{
    public class UpcomingSemestersSpec : BaseSpecification<Semester>
    {
        public UpcomingSemestersSpec()
            : base(s => s.StartDate > DateTime.UtcNow)
        {
            ApplyOrderBy(s => s.StartDate);
        }
    }
}
