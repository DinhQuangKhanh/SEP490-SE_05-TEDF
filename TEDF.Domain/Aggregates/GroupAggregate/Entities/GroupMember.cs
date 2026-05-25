using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Aggregates.GroupAggregate.Entities
{
    public class GroupMember : Entity<int>
    {
        public Guid GroupId { get; private set; }
        public Guid StudentId { get; private set; }
        public GroupMemberRole Role { get; private set; }
        public GroupMemberStatus Status { get; private set; }
        public DateTime JoinedAt { get; private set; }
        public DateTime? LeftAt { get; private set; }

        public bool IsActive => Status == GroupMemberStatus.Active;
        public bool IsLeader => Role == GroupMemberRole.Leader && IsActive;

        private GroupMember() { }

        internal static GroupMember Create(Guid groupId, Guid studentId, GroupMemberRole role)
        {
            return new GroupMember
            {
                GroupId = groupId,
                StudentId = studentId,
                Role = role,
                Status = GroupMemberStatus.Active,
                JoinedAt = DateTime.UtcNow
            };
        }

        public void Leave()
        {
            Status = GroupMemberStatus.Left;
            LeftAt = DateTime.UtcNow;
        }

        public void PromoteToLeader() => Role = GroupMemberRole.Leader;
        public void DemoteToMember() => Role = GroupMemberRole.Member;
    }
}
