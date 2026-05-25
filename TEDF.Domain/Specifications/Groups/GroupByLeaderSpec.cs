using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Specifications.Groups
{
    public class GroupByLeaderSpec : BaseSpecification<Group>
    {
        public GroupByLeaderSpec(Guid leaderId)
            : base(g => g.LeaderId == leaderId && g.Status == GroupStatus.Active)
        {
            AddInclude(g => g.Members);
        }
    }
}
