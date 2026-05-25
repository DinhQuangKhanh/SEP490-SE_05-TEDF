
namespace TEDF.Domain.Services
{
    public interface IGroupDomainService
    {
        /// <summary>
        /// Generates a unique group code.
        /// </summary>
        Task<string> GenerateGroupCodeAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a student can join a group.
        /// </summary>
        Task<(bool CanJoin, string? Reason)> CanStudentJoinGroupAsync(Guid studentId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets groups without a project assigned.
        /// </summary>
        Task<IEnumerable<Guid>> GetGroupsWithoutProjectAsync(int semesterId, CancellationToken cancellationToken = default);
    }
}
