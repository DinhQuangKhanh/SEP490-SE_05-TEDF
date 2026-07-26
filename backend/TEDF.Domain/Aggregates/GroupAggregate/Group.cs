using TEDF.Domain.Aggregates.GroupAggregate.Entities;
using TEDF.Domain.Aggregates.GroupAggregate.Events;
using TEDF.Domain.Aggregates.GroupAggregate.Rules;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Group;

namespace TEDF.Domain.Aggregates.GroupAggregate
{
    public class Group : AggregateRoot<Guid>
    {
        private readonly List<GroupMember> _members = [];
        private readonly List<GroupInvitation> _invitations = [];
        private readonly List<GroupJoinRequest> _joinRequests = [];

        private const int DefaultMaxMembers = 5;
        public const int MinMembers = 4;

        public GroupCode Code { get; private set; } = null!;

        /// <summary>
        /// Always the <c>SE_NN</c> tail of <see cref="Code"/> — never free text. Set only through
        /// <see cref="Create"/>, so it cannot drift away from the code.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>Optional nickname the students pick for themselves. Purely cosmetic.</summary>
        public string? DisplayName { get; private set; }
        public Guid? ProjectId { get; private set; }
        public int SemesterId { get; private set; }
        public Guid? LeaderId { get; private set; }
        public GroupStatus Status { get; private set; }
        public int MaxMembers { get; private set; }
        public bool IsOpenForRequests { get; private set; } = true;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();
        public IReadOnlyCollection<GroupInvitation> Invitations => _invitations.AsReadOnly();
        public IReadOnlyCollection<GroupJoinRequest> JoinRequests => _joinRequests.AsReadOnly();
        public GroupMember? Leader => _members.FirstOrDefault(m => m.IsLeader);

        /// <summary>
        /// Gets the count of currently active members (avoids materializing a list).
        /// </summary>
        public int ActiveMemberCount => _members.Count(m => m.IsActive);

        private Group() { }

        public static Group Create(GroupCode code, int semesterId, Guid leaderId, string? displayName = null, int maxMembers = DefaultMaxMembers)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Code = code,
                // Derived, never supplied: guarantees Name == the SE_NN tail of Code.
                Name = code.NamePart,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                SemesterId = semesterId,
                LeaderId = leaderId,
                Status = GroupStatus.Active,
                MaxMembers = maxMembers,
                CreatedAt = DateTime.UtcNow
            };

            var leaderMember = GroupMember.Create(group.Id, leaderId, GroupMemberRole.Leader);
            group._members.Add(leaderMember);

            group.RaiseDomainEvent(new GroupCreatedEvent(group.Id, group.Code.Value, semesterId));
            return group;
        }

        public void AddMember(Guid studentId)
        {
            CheckRule(new GroupCannotExceedMaxMembersRule(ActiveMemberCount, MaxMembers));
            CheckRule(new StudentCannotJoinMultipleGroupsRule(_members.Any(m => m.StudentId == studentId && m.IsActive)));

            var member = GroupMember.Create(Id, studentId, GroupMemberRole.Member);
            _members.Add(member);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new MemberAddedEvent(Id, studentId, GroupMemberRole.Member));
        }

        public void RemoveMember(Guid studentId, Guid removedBy)
        {
            var member = _members.FirstOrDefault(m => m.StudentId == studentId && m.IsActive)
                ?? throw new EntityNotFoundException(nameof(GroupMember), studentId);

            if (member.IsLeader)
                throw new BusinessRuleValidationException("Cannot remove the leader. Assign a new leader first.");

            member.Leave();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new MemberRemovedEvent(Id, studentId, removedBy));
        }

        public void ChangeLeader(Guid newLeaderId)
        {
            var newLeader = _members.FirstOrDefault(m => m.StudentId == newLeaderId && m.IsActive)
                ?? throw new BusinessRuleValidationException("New leader must be an active member of the group.");

            var oldLeader = Leader;
            oldLeader?.DemoteToMember();
            newLeader.PromoteToLeader();
            LeaderId = newLeaderId;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new LeaderChangedEvent(Id, oldLeader?.StudentId, newLeaderId));
        }

        public void AssignProject(Guid projectId)
        {
            if (ProjectId.HasValue)
                throw new BusinessRuleValidationException("Group already has an assigned project.");

            CheckRule(new GroupMustHaveMinMembersRule(ActiveMemberCount, MinMembers));
            ProjectId = projectId;
            IsOpenForRequests = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Disband()
        {
            if (Status != GroupStatus.Active)
                throw new BusinessRuleValidationException("Only active groups can be disbanded.");
            Status = GroupStatus.Disbanded;
            foreach (var member in _members.Where(m => m.IsActive)) member.Leave();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new GroupDisbandedEvent(Id));
        }

        public void Complete()
        {
            if (Status != GroupStatus.Active)
                throw new BusinessRuleValidationException("Only active groups can be completed.");
            Status = GroupStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new GroupCompletedEvent(Id));
        }

        /// <summary>
        /// Renames the students' nickname only. <see cref="Name"/> is derived from
        /// <see cref="Code"/> and is intentionally not settable.
        /// </summary>
        public void SetDisplayName(string? displayName)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetMaxMembers(int maxMembers)
        {
            var activeCount = ActiveMemberCount;
            if (maxMembers < activeCount)
                throw new BusinessRuleValidationException($"Cannot set max members to {maxMembers} when group has {activeCount} active members.");
            MaxMembers = maxMembers;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetOpenForRequests(bool isOpen) { IsOpenForRequests = isOpen; UpdatedAt = DateTime.UtcNow; }

        // ── Invitation Methods ──────────────────────────────────────────

        public GroupInvitation InviteMember(Guid inviterId, Guid inviteeId, string? message = null)
        {
            if (Status != GroupStatus.Active)
                throw new BusinessRuleValidationException("Can only invite members to an active group.");

            if (LeaderId != inviterId)
                throw new BusinessRuleValidationException("Only the group leader can invite members.");

            CheckRule(new GroupCannotExceedMaxMembersRule(ActiveMemberCount, MaxMembers));

            if (_members.Any(m => m.StudentId == inviteeId && m.IsActive))
                throw new BusinessRuleValidationException("Student is already an active member of this group.");

            if (_invitations.Any(i => i.InviteeId == inviteeId && i.IsPending))
                throw new BusinessRuleValidationException("A pending invitation already exists for this student.");

            var invitation = GroupInvitation.Create(Id, inviterId, inviteeId, message);
            _invitations.Add(invitation);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new MemberInvitedEvent(Id, Code.Value, inviterId, inviteeId));
            return invitation;
        }

        public void AcceptInvitation(int invitationId, Guid studentId)
        {
            var invitation = _invitations.FirstOrDefault(i => i.Id == invitationId && i.InviteeId == studentId)
                ?? throw new EntityNotFoundException(nameof(GroupInvitation), invitationId);

            if (!invitation.IsPending)
                throw new BusinessRuleValidationException("Invitation is no longer pending.");

            if (invitation.IsExpired)
            {
                invitation.Expire();
                throw new BusinessRuleValidationException("Invitation has expired.");
            }

            CheckRule(new GroupCannotExceedMaxMembersRule(ActiveMemberCount, MaxMembers));

            invitation.Accept();
            RaiseDomainEvent(new InvitationAcceptedEvent(Id, Code.Value, invitation.InviterId, studentId));
            AddMember(studentId);

            if (_invitations.Count != 0 && _members.Count == 5)
            {
                var invitationsRemainingToReject = _invitations.Where(j => j.GroupId == Id && j.IsPending).ToList();

                foreach (var invitationRemaining in invitationsRemainingToReject)
                {
                    invitationRemaining.Reject();
                    RaiseDomainEvent(new InvitationRejectedEvent(invitation.GroupId, Code.Value, invitation.InviterId, studentId));
                }
            }

        }

        public void RejectInvitation(int invitationId, Guid studentId)
        {
            var invitation = _invitations.FirstOrDefault(i => i.Id == invitationId && i.InviteeId == studentId)
                ?? throw new EntityNotFoundException(nameof(GroupInvitation), invitationId);

            if (!invitation.IsPending)
                throw new BusinessRuleValidationException("Invitation is no longer pending.");

            invitation.Reject();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new InvitationRejectedEvent(Id, Code.Value, invitation.InviterId, studentId));
        }

        // ── Join Request Methods ────────────────────────────────────────

        public GroupJoinRequest RequestToJoin(Guid studentId, string? message = null)
        {
            if (Status != GroupStatus.Active)
                throw new BusinessRuleValidationException("Can only request to join an active group.");

            if (!IsOpenForRequests)
                throw new BusinessRuleValidationException("This group is not accepting join requests.");

            CheckRule(new GroupCannotExceedMaxMembersRule(ActiveMemberCount, MaxMembers));

            if (_members.Any(m => m.StudentId == studentId && m.IsActive))
                throw new BusinessRuleValidationException("Student is already an active member of this group.");

            if (_joinRequests.Any(r => r.StudentId == studentId && r.IsPending))
                throw new BusinessRuleValidationException("A pending join request already exists for this student.");

            var request = GroupJoinRequest.Create(Id, studentId, message);
            _joinRequests.Add(request);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new JoinRequestedEvent(Id, Code.Value, studentId, LeaderId));
            return request;
        }

        public void ApproveJoinRequest(int requestId, Guid approverId)
        {
            if (LeaderId != approverId)
                throw new BusinessRuleValidationException("Only the group leader can approve join requests.");

            var request = _joinRequests.FirstOrDefault(r => r.Id == requestId)
                ?? throw new EntityNotFoundException(nameof(GroupJoinRequest), requestId);

            if (!request.IsPending)
                throw new BusinessRuleValidationException("Join request is no longer pending.");

            if (request.IsExpired)
            {
                request.Reject();
                UpdatedAt = DateTime.UtcNow;
                throw new BusinessRuleValidationException("Join request has expired.");
            }

            CheckRule(new GroupCannotExceedMaxMembersRule(ActiveMemberCount, MaxMembers)); 

            request.Approve();
            
            RaiseDomainEvent(new JoinRequestApprovedEvent(Id, Code.Value, request.StudentId));
            AddMember(request.StudentId);

            if (_joinRequests.Count != 0 && _members.Count == 5)
            {
                var joinRequestRemainingToReject = _joinRequests.Where(j => j.GroupId == Id && j.IsPending).ToList();

                foreach (var joinRequest in joinRequestRemainingToReject)
                {
                    joinRequest.Reject();
                    RaiseDomainEvent(new JoinRequestRejectedEvent(joinRequest.GroupId, Code.Value, joinRequest.StudentId));
                }
            }
        }

        public void RejectJoinRequest(int requestId, Guid rejecterId)
        {
            if (LeaderId != rejecterId)
                throw new BusinessRuleValidationException("Only the group leader can reject join requests.");

            var request = _joinRequests.FirstOrDefault(r => r.Id == requestId)
                ?? throw new EntityNotFoundException(nameof(GroupJoinRequest), requestId);

            if (!request.IsPending)
                throw new BusinessRuleValidationException("Join request is no longer pending.");

            request.Reject();
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new JoinRequestRejectedEvent(Id, Code.Value, request.StudentId));
        }
    }
}
