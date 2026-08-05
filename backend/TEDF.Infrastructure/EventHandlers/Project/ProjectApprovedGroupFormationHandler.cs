using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Interfaces;
using TEDF.Infrastructure.Caching;
// The enclosing namespaces are also named Project/Group, so the aggregates are aliased.
using GroupAggregate = TEDF.Domain.Aggregates.GroupAggregate.Group;

namespace TEDF.Infrastructure.EventHandlers.Project
{
    /// <summary>
    /// Materializes the group a mentor listed on the register form once their topic passes
    /// evaluation. Runs alongside <see cref="ProjectApprovedEventHandler"/>.
    /// </summary>
    /// <remarks>
    /// Group formation is best-effort: topics proposed without a roster — the common case — and
    /// rosters that no longer hold (a student joined another group meanwhile) simply leave the
    /// topic on the normal pool flow, where student groups register for it themselves.
    /// </remarks>
    public class ProjectApprovedGroupFormationHandler : INotificationHandler<ProjectApprovedEvent>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProjectApprovedGroupFormationHandler> _logger;

        public ProjectApprovedGroupFormationHandler(
            IProjectRepository projectRepository,
            IGroupRepository groupRepository,
            ISemesterRepository semesterRepository,
            ICacheInvalidationService cacheInvalidation,
            IUnitOfWork unitOfWork,
            ILogger<ProjectApprovedGroupFormationHandler> logger)
        {
            _projectRepository = projectRepository;
            _groupRepository = groupRepository;
            _semesterRepository = semesterRepository;
            _cacheInvalidation = cacheInvalidation;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(ProjectApprovedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var project = await _projectRepository.GetWithProposedMembersAsync(notification.ProjectId, cancellationToken);
                if (project is null)
                    return;

                // Nothing to do for the ordinary flow: no roster, or a group already registered.
                if (project.ProposedMembers.Count == 0 || project.GroupId.HasValue)
                    return;

                var leaderId = project.ProposedMembers.FirstOrDefault(m => m.IsLeader)?.StudentId;
                if (leaderId is null)
                {
                    _logger.LogWarning(
                        "Project {ProjectId} has a proposed roster without a leader; skipping group creation.",
                        project.Id);
                    return;
                }

                // A group cannot be assigned a project below the minimum size, so an under-filled
                // register form leaves the topic on the normal pool flow instead of half-forming it.
                if (project.ProposedMembers.Count < GroupAggregate.MinMembers)
                {
                    _logger.LogWarning(
                        "Project {ProjectId}'s register form lists {Count} students, fewer than the {Min} required; skipping group creation.",
                        project.Id, project.ProposedMembers.Count, GroupAggregate.MinMembers);
                    return;
                }

                var semester = await _semesterRepository.GetByIdAsync(project.SemesterId, cancellationToken);
                if (semester is null)
                {
                    _logger.LogWarning(
                        "Semester {SemesterId} for project {ProjectId} was not found; skipping group creation.",
                        project.SemesterId, project.Id);
                    return;
                }

                // Any member already placed in a group invalidates the whole roster — partially
                // forming the group would silently drop a student the mentor registered.
                foreach (var member in project.ProposedMembers)
                {
                    if (await _groupRepository.IsStudentInActiveGroupAsync(member.StudentId, project.SemesterId, cancellationToken))
                    {
                        _logger.LogWarning(
                            "Student {StudentId} on project {ProjectId}'s roster already belongs to a group this semester; skipping group creation.",
                            member.StudentId, project.Id);
                        return;
                    }
                }

                var sequence = await _groupRepository.GetNextSequenceAsync(project.SemesterId, cancellationToken);
                var code = GroupCode.Generate(semester.Code.Value, sequence);

                var group = GroupAggregate.Create(code, project.SemesterId, leaderId.Value, maxMembers: project.MaxStudents);
                foreach (var member in project.ProposedMembers.Where(m => m.StudentId != leaderId.Value))
                {
                    group.AddMember(member.StudentId);
                }

                group.AssignProject(project.Id);
                project.AssignGroup(group.Id);

                await _groupRepository.AddAsync(group, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _cacheInvalidation.InvalidateGroupCacheAsync(group.Id, cancellationToken);

                _logger.LogInformation(
                    "Created group {GroupCode} from the register form of project {ProjectId} with {Count} members.",
                    code.Value, project.Id, group.Members.Count);
            }
            catch (Exception ex)
            {
                // The topic stays approved and simply remains open for normal registration.
                _logger.LogError(ex, "Could not form the proposed group for project {ProjectId}.", notification.ProjectId);
            }
        }
    }
}
