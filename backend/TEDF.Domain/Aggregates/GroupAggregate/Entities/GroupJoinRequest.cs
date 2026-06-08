using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Aggregates.GroupAggregate.Entities
{
    public class GroupJoinRequest : Entity<int>
    {
        public Guid GroupId { get; private set; }
        public Guid StudentId { get; private set; }
        public string? Message { get; private set; }
        public GroupJoinRequestStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RespondedAt { get; private set; }

        public bool IsPending => Status == GroupJoinRequestStatus.Pending;
        public bool IsExpired => Status == GroupJoinRequestStatus.Pending && DateTime.UtcNow > ExpiresAt;

        private GroupJoinRequest() { }

        internal static GroupJoinRequest Create(Guid groupId, Guid studentId, string? message = null, int expirationHours = 1)
        {
            return new GroupJoinRequest
            {
                GroupId = groupId,
                StudentId = studentId,
                Message = message,
                Status = GroupJoinRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours)
            };
        }

        public void Approve()
        {
            Status = GroupJoinRequestStatus.Approved;
            RespondedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = GroupJoinRequestStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }
    }
}
