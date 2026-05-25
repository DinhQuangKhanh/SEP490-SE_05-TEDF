using TEDF.Domain.Aggregates.GroupAggregate;

namespace TEDF.Domain.Specifications.Groups
{
    public class GroupByStudentSpec : BaseSpecification<Group>
    {
        public GroupByStudentSpec(Guid studentId, int? semesterId = null)
            : base(g => g.Members.Any(m => m.StudentId == studentId && m.IsActive) &&
                        (!semesterId.HasValue || g.SemesterId == semesterId.Value))
        {
            AddInclude(g => g.Members);
        }
    }
}
