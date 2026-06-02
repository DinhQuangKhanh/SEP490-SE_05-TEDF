using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices
{
    public class GroupDomainService : IGroupDomainService
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IProjectRepository _projectRepository;

        public GroupDomainService(
            IGroupRepository groupRepository,
            IProjectRepository projectRepository)
        {
            _groupRepository = groupRepository;
            _projectRepository = projectRepository;
        }

        public async Task<string> GenerateGroupCodeAsync(int year, CancellationToken ct = default)
        {
            var sequence = await _groupRepository.GetNextSequenceAsync(year, ct);
            return $"G-{year}-{sequence:D4}";
        }

        public async Task<(bool CanJoin, string? Reason)> CanStudentJoinGroupAsync(Guid studentId, int semesterId, CancellationToken ct = default)
        {
            var isInActiveGroup = await _groupRepository.IsStudentInActiveGroupAsync(studentId, semesterId, ct);
            if (isInActiveGroup)
                return (false, "Sinh viên đã tham gia một nhóm khác trong học kỳ này.");

            return (true, null);
        }

        public async Task<IEnumerable<Guid>> GetGroupsWithoutProjectAsync(int semesterId, CancellationToken ct = default)
        {
            return await _groupRepository.GetActiveGroupIdsWithoutProjectAsync(semesterId, ct);
        }
    }
}
