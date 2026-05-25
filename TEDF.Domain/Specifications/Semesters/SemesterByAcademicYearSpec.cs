using TEDF.Domain.Aggregates.SemesterAggregate;

namespace TEDF.Domain.Specifications.Semesters
{
    public class SemesterByAcademicYearSpec : BaseSpecification<Semester>
    {
        public SemesterByAcademicYearSpec(string academicYear)
            : base(s => s.AcademicYear.Value == academicYear)
        {
            AddInclude(s => s.Phases);
            ApplyOrderBy(s => s.StartDate);
        }
    }
}
