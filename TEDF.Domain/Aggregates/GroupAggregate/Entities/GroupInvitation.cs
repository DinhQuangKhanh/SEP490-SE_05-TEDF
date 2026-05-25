using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Aggregates.GroupAggregate.Entities
{
    public class GroupInvitation : Entity<int>
    {
        public Guid GroupId { get; private set; }
        public Guid InviterId { get; private set; }
        public Guid InviteeId { get; private set; }
        public GroupInvitationStatus Status { get; private set; }
        public string? Message { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RespondedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }

        public bool IsExpired => Status == GroupInvitationStatus.Pending && DateTime.UtcNow > ExpiresAt;
        public bool IsPending => Status == GroupInvitationStatus.Pending && !IsExpired;

        private GroupInvitation() { }

        internal static GroupInvitation Create(Guid groupId, Guid inviterId, Guid inviteeId, string? message = null, int expirationDays = 7)
        {
            return new GroupInvitation
            {
                GroupId = groupId,
                InviterId = inviterId,
                InviteeId = inviteeId,
                Status = GroupInvitationStatus.Pending,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
            };
        }

        public void Accept()
        {
            Status = GroupInvitationStatus.Accepted;
            RespondedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            Status = GroupInvitationStatus.Rejected;
            RespondedAt = DateTime.UtcNow;
        }

        public void Expire()
        {
            Status = GroupInvitationStatus.Expired;
        }
    }
}
