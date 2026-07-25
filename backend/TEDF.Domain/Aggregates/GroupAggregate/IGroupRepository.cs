using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.GroupAggregate
{
    public interface IGroupRepository : IRepository<Group, Guid>
    {
        Task<Group?> GetByCodeAsync(GroupCode code, CancellationToken cancellationToken = default);
        Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Group>> GetBySemesterIdAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Group>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
        Task<Group?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<bool> ExistsCodeAsync(GroupCode code, CancellationToken cancellationToken = default);
        /// <summary>
        /// Next free <c>SE_NN</c> sequence within a semester. Group numbering restarts at 1 each
        /// semester because the code is scoped by semester code.
        /// </summary>
        Task<int> GetNextSequenceAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<bool> IsStudentInActiveGroupAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);
        Task<bool> HasPendingJoinRequestAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);
        Task<bool> IsLeaderOfGroupAsync(Guid leaderId, Guid groupId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetActiveGroupIdsWithoutProjectAsync(int semesterId, CancellationToken cancellationToken = default);
        Task<Group?> GetWithInvitationsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Group?> GetWithJoinRequestsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Group?> GetWithJoinRequestsAndInvitationsAsync(Guid id, CancellationToken cancellationToken);
        Task<Group?> GetWithAllRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
