using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.Rules;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Group;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.TopicPool;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Domain service implementation for topic pool-related business logic.
/// </summary>
public class TopicPoolsDomainService : ITopicPoolsDomainService
{
    private readonly ITopicPoolRepository _topicPoolRepository;
    private readonly ITopicRegistrationRepository _registrationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMajorReadRepository _majorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRegisterFormParser _registerFormParser;
    private readonly ISemestersDomainService _semesterDomainService;
    private readonly IProjectsDomainService _projectsDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TopicPoolsDomainService> _logger;

    public TopicPoolsDomainService(
        ITopicPoolRepository topicPoolRepository,
        ITopicRegistrationRepository registrationRepository,
        IProjectRepository projectRepository,
        IGroupRepository groupRepository,
        IMajorReadRepository majorRepository,
        IUserRepository userRepository,
        IRegisterFormParser registerFormParser,
        ISemestersDomainService semesterDomainService,
        IProjectsDomainService projectsDomainService,
        IUnitOfWork unitOfWork,
        ILogger<TopicPoolsDomainService> logger)
    {
        _projectsDomainService = projectsDomainService;
        _topicPoolRepository = topicPoolRepository;
        _registrationRepository = registrationRepository;
        _projectRepository = projectRepository;
        _groupRepository = groupRepository;
        _majorRepository = majorRepository;
        _userRepository = userRepository;
        _registerFormParser = registerFormParser;
        _semesterDomainService = semesterDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> GeneratePoolCodeAsync(int majorId, CancellationToken cancellationToken = default)
    {
        var major = await _majorRepository.GetByIdAsync(majorId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Major), majorId);

        return TopicPool.GenerateCode(major.Code);
    }

    public async Task<TopicPool> GetOrCreatePoolAsync(int majorId, Guid createdBy, CancellationToken cancellationToken = default)
    {
        // Try to get existing pool
        var existingPool = await _topicPoolRepository.GetByMajorIdAsync(majorId, cancellationToken);
        if (existingPool is not null)
            return existingPool;

        // Create new pool
        var major = await _majorRepository.GetByIdAsync(majorId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Major), majorId);

        var code = TopicPool.GenerateCode(major.Code);
        var name = $"Kho đề tài {major.Name}";

        var pool = TopicPool.Create(code, name, null, majorId, createdBy: createdBy);

        await _topicPoolRepository.AddAsync(pool, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created topic pool {PoolCode} for major {MajorId}", code, majorId);

        return pool;
    }

    public async Task<int> GetMentorActiveTopicCountAsync(Guid mentorId, Guid topicPoolId, CancellationToken cancellationToken = default)
    {
        // Active = Available or Reserved (not Assigned or Expired)
        return await _projectRepository.CountActivePoolTopicsByMentorAsync(topicPoolId, mentorId, cancellationToken);
    }

    public async Task<(bool CanPropose, string? Reason)> CanMentorProposeTopicAsync(
        Guid mentorId,
        Guid topicPoolId,
        CancellationToken cancellationToken = default)
    {
        var pool = await _topicPoolRepository.GetByIdAsync(topicPoolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), topicPoolId);

        if (!pool.IsAcceptingProposals())
            return (false, "Topic pool is not currently accepting proposals.");

        var currentCount = await GetMentorActiveTopicCountAsync(mentorId, topicPoolId, cancellationToken);
        if (currentCount >= pool.MaxActiveTopicsPerMentor)
            return (false, $"You have reached the maximum of {pool.MaxActiveTopicsPerMentor} active topics in this pool.");

        return (true, null);
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

        if (project.PoolStatus != PoolTopicStatus.Available)
            throw new BusinessRuleValidationException("This topic is not available for registration.");

        if (project.Status != ProjectStatus.Approved)
            throw new BusinessRuleValidationException("Only approved topics can be registered for.");

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

    public async Task<TopicPoolStatistics> GetPoolStatisticsAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
    {
        var pool = await _topicPoolRepository.GetByIdAsync(topicPoolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), topicPoolId);

        var statusCounts = await _projectRepository.GetPoolStatusCountsAsync(topicPoolId, cancellationToken);
        var totalTopics = statusCounts.Values.Sum();

        var projectIds = await _projectRepository.GetPoolProjectIdsAsync(topicPoolId, cancellationToken);

        var registrationCounts = await _registrationRepository.GetRegistrationStatusCountsByProjectIdsAsync(projectIds, cancellationToken);

        var mentorCounts = await _projectRepository.GetMentorTopicCountsInPoolAsync(topicPoolId, cancellationToken);

        return new TopicPoolStatistics(
            TotalTopics: totalTopics,
            AvailableTopics: statusCounts.GetValueOrDefault(PoolTopicStatus.Available),
            ReservedTopics: statusCounts.GetValueOrDefault(PoolTopicStatus.Reserved),
            AssignedTopics: statusCounts.GetValueOrDefault(PoolTopicStatus.Assigned),
            ExpiredTopics: statusCounts.GetValueOrDefault(PoolTopicStatus.Expired),
            TotalRegistrations: registrationCounts.Values.Sum(),
            PendingRegistrations: registrationCounts.GetValueOrDefault(TopicRegistrationStatus.Pending),
            ConfirmedRegistrations: registrationCounts.GetValueOrDefault(TopicRegistrationStatus.Confirmed),
            TopMentorTopicCount: mentorCounts.Count > 0 ? mentorCounts.Max() : 0,
            AverageTopicsPerMentor: mentorCounts.Count > 0 ? mentorCounts.Average() : 0
        );
    }

    public async Task<int> ExpireOldTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default)
    {
        // Find all projects that should be expired
        var expiredProjects = await _projectRepository.GetExpirablePoolTopicsAsync(currentSemesterId, cancellationToken);

        foreach (var project in expiredProjects)
        {
            project.MarkAsExpired();
            _projectRepository.Update(project);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} topics in semester {SemesterId}", expiredProjects.Count, currentSemesterId);

        return expiredProjects.Count;
    }

    public async Task<IEnumerable<Guid>> GetAvailableTopicsInPoolAsync(Guid topicPoolId, CancellationToken cancellationToken = default)
    {
        return await _projectRepository.GetAvailableApprovedPoolTopicIdsAsync(topicPoolId, cancellationToken);
    }

    public async Task<IEnumerable<Project>> GetExpiringTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default)
    {
        return await _projectRepository.GetExpiringPoolTopicsWithMentorsAsync(currentSemesterId, cancellationToken);
    }

    public async Task<int> ResolveMissingExpirationSemestersAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _projectRepository.GetPoolTopicsMissingExpirationAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return 0;
        }

        var resolvedCount = 0;
        foreach (var project in candidates)
        {
            if (!project.TopicPoolId.HasValue || !project.CreatedInSemesterId.HasValue)
            {
                continue;
            }

            var pool = await _topicPoolRepository.GetByIdAsync(project.TopicPoolId.Value, cancellationToken);
            if (pool is null)
            {
                continue;
            }

            var expirationOffset = Math.Max(1, pool.ExpirationSemesters);
            var expirationSemesterId = await _semesterDomainService.GetSemesterAfterAsync(
                project.CreatedInSemesterId.Value,
                expirationOffset,
                cancellationToken);

            if (!expirationSemesterId.HasValue)
            {
                continue;
            }

            project.SetExpirationSemester(expirationSemesterId.Value);
            _projectRepository.Update(project);
            resolvedCount++;
        }

        if (resolvedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Resolved expiration semester for {Count} pool topics.", resolvedCount);
        return resolvedCount;
    }

    public async Task<int?> CalculateExpirationSemesterAsync(int createdSemesterId, int expirationSemesters, CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(1, expirationSemesters);
        return await _semesterDomainService.GetSemesterAfterAsync(createdSemesterId, offset, cancellationToken);
    }

    // ── TopicPools feature write operations (moved in from command handlers) ──

    public async Task<Guid> ProposeTopicAsync(Guid poolId, Guid mentorId, PoolTopicContent content, byte[] registerForm, CancellationToken cancellationToken = default)
    {
        if (registerForm is null || registerForm.Length == 0)
            throw new BusinessRuleValidationException("Phiếu đăng ký (Capstone Project Register) là bắt buộc khi đề xuất đề tài.");

        var pool = await _topicPoolRepository.GetByIdAsync(poolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), poolId);

        if (!pool.IsAcceptingProposals())
            throw new BusinessRuleValidationException("This topic pool is not currently accepting new topic proposals.");

        var createdSemesterId = await _semesterDomainService.GetActiveSemesterIdAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException("Không tìm thấy học kỳ đang hoạt động để tạo đề tài từ topic pool.");

        var code = await _projectsDomainService.GenerateProjectCodeAsync(createdSemesterId, pool.MajorId, cancellationToken);

        var expirationOffset = Math.Max(1, pool.ExpirationSemesters);
        var expirationSemesterId = await _semesterDomainService.GetSemesterAfterAsync(createdSemesterId, expirationOffset, cancellationToken);

        var project = Project.CreateFromPool(
            code: code,
            nameVi: ProjectName.Create(content.NameVi),
            nameEn: ProjectName.Create(content.NameEn),
            nameAbbr: content.NameAbbr,
            description: content.Description,
            objectives: content.Objectives,
            scope: content.Scope,
            technologyStack: content.Technologies is not null ? TechnologyStack.Create(content.Technologies) : null,
            expectedResults: content.ExpectedResults,
            majorId: pool.MajorId,
            semesterId: createdSemesterId,
            maxStudents: content.MaxStudents,
            topicPoolId: pool.Id,
            expirationSemesterId: expirationSemesterId);

        project.AddMentor(mentorId, assignedBy: mentorId);

        var roster = await ResolveRosterAsync(registerForm, project.MaxStudents, cancellationToken);
        if (roster.Count > 0)
            project.SetProposedRoster(roster);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }

    /// <summary>
    /// Turns the attached register form into student ids. Rows that cannot be matched to a user are
    /// skipped rather than rejected — the roster is an optional convenience, not a gate on proposing.
    /// Attaching the form is required (guarded by the caller); reading its contents is not.
    /// </summary>
    private async Task<List<(Guid StudentId, bool IsLeader)>> ResolveRosterAsync(
        byte[] registerForm, int maxStudents, CancellationToken cancellationToken)
    {
        var roster = new List<(Guid StudentId, bool IsLeader)>();

        using var stream = new MemoryStream(registerForm, writable: false);
        var rows = _registerFormParser.ExtractRoster(stream);
        if (rows.Count == 0)
            return roster;

        foreach (var row in rows)
        {
            var user = await FindStudentAsync(row, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning(
                    "Register form lists a student that is not in the system (code {Code}); skipping the row.",
                    row.StudentCode ?? row.Email);
                continue;
            }

            if (roster.Any(r => r.StudentId == user.Id))
                continue;

            roster.Add((user.Id, row.IsLeader));
        }

        if (roster.Count > maxStudents)
        {
            _logger.LogWarning(
                "Register form lists {Count} students but the topic allows {Max}; ignoring the roster.",
                roster.Count, maxStudents);
            return [];
        }

        return roster;
    }

    private async Task<Domain.Aggregates.UserAggregate.User?> FindStudentAsync(
        RegisterRosterRow row, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(row.StudentCode))
        {
            var byCode = await _userRepository.GetByStudentCodeAsync(row.StudentCode, cancellationToken);
            if (byCode is not null)
                return byCode;
        }

        if (!string.IsNullOrWhiteSpace(row.Email))
            return await _userRepository.GetByEmailAsync(row.Email, cancellationToken);

        return null;
    }

    public async Task UpdatePoolTopicAsync(Guid projectId, PoolTopicContent content, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (project.SourceType != ProjectSourceType.FromPool)
            throw new BusinessRuleValidationException(
                "Chỉ đề tài trong kho mới có thể được giảng viên chỉnh sửa qua chức năng này.");

        if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.NeedsModification)
            throw new BusinessRuleValidationException(
                "Đề tài chỉ có thể chỉnh sửa khi ở trạng thái Nháp hoặc Yêu cầu chỉnh sửa.");

        project.UpdateBasicInfo(
            nameVi: ProjectName.Create(content.NameVi),
            nameEn: ProjectName.Create(content.NameEn),
            nameAbbr: content.NameAbbr,
            description: content.Description,
            objectives: content.Objectives,
            scope: content.Scope,
            technologies: content.Technologies,
            expectedResults: content.ExpectedResults);

        project.SetMaxStudents(content.MaxStudents);

        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResubmitPoolTopicAsync(Guid projectId, Guid mentorId, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        if (project.SourceType != ProjectSourceType.FromPool)
            throw new BusinessRuleValidationException(
                "Chỉ đề tài trong kho mới có thể được giảng viên gửi thẩm định qua chức năng này.");

        if (project.Status == ProjectStatus.Draft && project.EvaluationCount == 0)
            project.SubmitForEvaluation(mentorId);
        else
            project.Resubmit(mentorId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
