using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Specifications.Groups
{
    public class GroupWithoutProjectSpec : BaseSpecification<Group>
    {
        public GroupWithoutProjectSpec(int semesterId)
            : base(g => g.SemesterId == semesterId &&
                        g.Status == GroupStatus.Active &&
                        g.ProjectId == null)
        {
            AddInclude(g => g.Members);
            ApplyOrderBy(g => g.CreatedAt);
        }
    }
}
