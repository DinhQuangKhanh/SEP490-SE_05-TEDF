using TEDF.Domain.Aggregates.GroupAggregate;

namespace TEDF.Domain.Specifications.Groups
{
    public class GroupBySemesterSpec : BaseSpecification<Group>
    {
        public GroupBySemesterSpec(int semesterId, bool includeMembers = false)
            : base(g => g.SemesterId == semesterId)
        {
            if (includeMembers)
            {
                AddInclude(g => g.Members);
            }
            ApplyOrderByDescending(g => g.CreatedAt);
        }
    }
}
