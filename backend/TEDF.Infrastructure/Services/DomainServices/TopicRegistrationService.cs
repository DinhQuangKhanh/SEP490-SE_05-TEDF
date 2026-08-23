using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.TopicPool;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// The pool-registration lifecycle: a group requests a topic, the mentor confirms/rejects it, or the
/// group leader cancels it. Extracted from the former god-service (single responsibility).
/// </summary>
public sealed class TopicRegistrationService : ITopicRegistrationService
{
    private readonly ITopicRegistrationRepository _registrationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ITopicPoolRepository _topicPoolRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TopicRegistrationService> _logger;

    public TopicRegistrationService(
        ITopicRegistrationRepository registrationRepository,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        ITopicPoolRepository topicPoolRepository,
        IUnitOfWork unitOfWork,
        ILogger<TopicRegistrationService> logger)
    {
        _registrationRepository = registrationRepository;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _topicPoolRepository = topicPoolRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TopicRegistration> RequestRegistrationAsync(
        Guid projectId,
        Guid groupId,
        Guid registeredBy,
        int priority = 1,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        if (group.Status != GroupStatus.Active)
            throw new BusinessRuleValidationException("Only active groups can register pool topics.");

        if (group.LeaderId != registeredBy)
            throw new BusinessRuleValidationException("Only the group leader can register a topic from the pool.");

        if (group.ProjectId.HasValue)
            throw new BusinessRuleValidationException("This group already has an assigned project.");

        // A group may only have one pending registration at a time — to register another topic
        // it must cancel the previous request first.
        var groupRegistrations = await _registrationRepository.GetByGroupIdAsync(groupId, cancellationToken);
        if (groupRegistrations.Any(r => r.Status == TopicRegistrationStatus.Pending))
            throw new BusinessRuleValidationException(
                "Nhóm đang có một yêu cầu đăng ký chờ duyệt. Hãy huỷ yêu cầu đó trước khi đăng ký đề tài khác.");

        var project = await _projectRepository.GetWithMentorsAsync(projectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        // Validate project
        if (project.SourceType != ProjectSourceType.FromPool)
            throw new BusinessRuleValidationException("This project is not from the topic pool.");

        // Only an approved topic may be registered — checked before availability so a not-yet-reviewed
        // topic returns the accurate reason (a pool topic is Available from proposal time onward).
        if (project.Status != ProjectStatus.Approved)
            throw new BusinessRuleValidationException("Chỉ có thể đăng ký đề tài đã được duyệt.");

        if (project.PoolStatus != PoolTopicStatus.Available)
            throw new BusinessRuleValidationException("Đề tài này hiện không mở đăng ký.");

        // Group and topic must run in the same semester. Without this the mismatch stays invisible
        // until much later — it is how topics stamped with the wrong semester went unnoticed.
        if (group.SemesterId != project.SemesterId)
            throw new BusinessRuleValidationException("Nhóm và đề tài không thuộc cùng một học kỳ.");

        // Block registration when the topic's mentor already supervises the max number of groups
        // this semester. Unlike the proposal screen, pending pool registrations are NOT counted here.
        var topicMentorId = project.Mentors.FirstOrDefault(m => m.IsActive)?.MentorId;
        if (topicMentorId.HasValue)
        {
            var mentorActiveGroups = await _projectRepository.CountMentorActiveProjectsInSemesterAsync(
                topicMentorId.Value, project.SemesterId, cancellationToken);
            if (mentorActiveGroups >= MentorCannotExceedMaxGroupsPerSemesterRule.MaxGroupsPerSemester)
                throw new BusinessRuleValidationException(
                    $"Giảng viên của đề tài đã đủ {MentorCannotExceedMaxGroupsPerSemesterRule.MaxGroupsPerSemester} nhóm hướng dẫn, không thể đăng ký đề tài này.");
        }

        if (!project.TopicPoolId.HasValue)
            throw new BusinessRuleValidationException("Project is not associated with a topic pool.");

        var pool = await _topicPoolRepository.GetByIdAsync(project.TopicPoolId.Value, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), project.TopicPoolId.Value);

        if (!pool.IsAcceptingRegistrations())
            throw new BusinessRuleValidationException("Topic pool is not currently accepting registrations.");

        // Check duplicate
        var hasPending = await _registrationRepository.HasPendingRegistrationAsync(groupId, projectId, cancellationToken);
        if (hasPending)
            throw new BusinessRuleValidationException("Your group already has a pending registration for this topic.");

        // Create registration
        var registration = TopicRegistration.Create(projectId, groupId, registeredBy, priority, note);
        await _registrationRepository.AddAsync(registration, cancellationToken);

        // Update project status to Reserved
        project.SetPoolStatus(PoolTopicStatus.Reserved);
        _projectRepository.Update(project);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registration requested: Project {ProjectId}, Group {GroupId}", projectId, groupId);

        return registration;
    }

    public async Task ConfirmRegistrationAsync(
        Guid registrationId,
        Guid confirmedBy,
        CancellationToken cancellationToken = default)
    {
        var registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicRegistration), registrationId);

        if (registration.Status != TopicRegistrationStatus.Pending)
            throw new BusinessRuleValidationException("Only pending registrations can be confirmed.");

        var project = await _projectRepository.GetByIdAsync(registration.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), registration.ProjectId);

        // Confirm the registration
        registration.Confirm(confirmedBy);
        _registrationRepository.Update(registration);

        // Assign group to project and update status
        project.AssignGroup(registration.GroupId);
        project.SetPoolStatus(PoolTopicStatus.Assigned);
        _projectRepository.Update(project);

        // Assign project to group (sets ProjectId + closes join requests).
        // Must load members so the "min members" rule in AssignProject sees the real count.
        var group = await _groupRepository.GetWithMembersAsync(registration.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), registration.GroupId);
        group.AssignProject(project.Id);
        _groupRepository.Update(group);

        // Cancel all other pending registrations for this project
        var otherPendingRegistrations = await _registrationRepository.GetPendingByProjectIdAsync(registration.ProjectId, cancellationToken);
        foreach (var otherReg in otherPendingRegistrations.Where(r => r.Id != registrationId))
        {
            otherReg.Cancel("Another group was selected for this topic.");
            _registrationRepository.Update(otherReg);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registration confirmed: {RegistrationId}, Group {GroupId} assigned to Project {ProjectId}",
            registrationId, registration.GroupId, registration.ProjectId);
    }

    public async Task RejectRegistrationAsync(
        Guid registrationId,
        Guid rejectedBy,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicRegistration), registrationId);

        if (registration.Status != TopicRegistrationStatus.Pending)
            throw new BusinessRuleValidationException("Only pending registrations can be rejected.");

        registration.Reject(rejectedBy, reason);
        _registrationRepository.Update(registration);

        // Check if there are any other pending registrations for this project
        var otherPendingCount = await _registrationRepository.CountPendingByProjectIdExcludingAsync(
            registration.ProjectId, registrationId, cancellationToken);

        // If no other pending registrations, set project back to Available
        if (otherPendingCount == 0)
        {
            var project = await _projectRepository.GetByIdAsync(registration.ProjectId, cancellationToken);
            if (project is not null && project.PoolStatus == PoolTopicStatus.Reserved)
            {
                project.SetPoolStatus(PoolTopicStatus.Available);
                _projectRepository.Update(project);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registration rejected: {RegistrationId}, Reason: {Reason}", registrationId, reason);
    }

    public async Task CancelRegistrationAsync(
        Guid registrationId,
        Guid cancelledBy,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicRegistration), registrationId);

        if (registration.Status != TopicRegistrationStatus.Pending)
            throw new BusinessRuleValidationException("Only pending registrations can be cancelled.");

        // Authorization: only the leader of the registering group may cancel.
        var group = await _groupRepository.GetByIdAsync(registration.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), registration.GroupId);

        if (group.LeaderId != cancelledBy)
            throw new BusinessRuleValidationException("Only the group leader can cancel the registration.");

        registration.Cancel(reason);
        _registrationRepository.Update(registration);

        // If no other pending registrations remain, free the topic back to Available
        // (RequestRegistration had reserved it).
        var otherPendingCount = await _registrationRepository.CountPendingByProjectIdExcludingAsync(
            registration.ProjectId, registrationId, cancellationToken);

        if (otherPendingCount == 0)
        {
            var project = await _projectRepository.GetByIdAsync(registration.ProjectId, cancellationToken);
            if (project is not null && project.PoolStatus == PoolTopicStatus.Reserved)
            {
                project.SetPoolStatus(PoolTopicStatus.Available);
                _projectRepository.Update(project);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registration cancelled: {RegistrationId}", registrationId);
    }
}
