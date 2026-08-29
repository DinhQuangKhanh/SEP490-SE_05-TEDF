using Microsoft.Extensions.Logging;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Rules;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Services;
using TEDF.Infrastructure.Services.RegisterForm;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Proposal lifecycle for pool topics: validate the register form, create the topic, edit a draft and
/// resubmit. Extracted from the former god-service so it carries only the dependencies it needs.
/// </summary>
public sealed class TopicProposalService : ITopicProposalService
{
    private readonly ITopicPoolRepository _topicPoolRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRegisterFormParser _registerFormParser;
    private readonly ISemestersDomainService _semesterDomainService;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IProjectsDomainService _projectsDomainService;
    private readonly ISystemSettingsService _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TopicProposalService> _logger;

    /// <summary>Default group capacity for a proposed topic (the register form does not carry it).</summary>
    private const int DefaultMaxStudents = 5;

    public TopicProposalService(
        ITopicPoolRepository topicPoolRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IRegisterFormParser registerFormParser,
        ISemestersDomainService semesterDomainService,
        ISemesterRepository semesterRepository,
        IProjectsDomainService projectsDomainService,
        ISystemSettingsService settings,
        IUnitOfWork unitOfWork,
        ILogger<TopicProposalService> logger)
    {
        _topicPoolRepository = topicPoolRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _registerFormParser = registerFormParser;
        _semesterDomainService = semesterDomainService;
        _semesterRepository = semesterRepository;
        _projectsDomainService = projectsDomainService;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<(bool CanPropose, string? Reason)> CanMentorProposeTopicAsync(
        Guid mentorId,
        Guid topicPoolId,
        CancellationToken cancellationToken = default)
    {
        var pool = await _topicPoolRepository.GetByIdAsync(topicPoolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), topicPoolId);

        if (!pool.IsAcceptingProposals())
            return (false, "Kho đề tài hiện không nhận đề xuất.");

        var rule = await BuildMentorTopicLimitRuleAsync(mentorId, pool, cancellationToken);
        if (rule.IsBroken())
            return (false, rule.Message);

        return (true, null);
    }

    // Internal write-flow helper (count for the MaxTopicsPerMentor guard) — never a display read.
    private async Task<int> GetMentorActiveTopicCountAsync(Guid mentorId, Guid topicPoolId, CancellationToken cancellationToken)
    {
        // Active = Available or Reserved (not Assigned or Expired)
        return await _projectRepository.CountActivePoolTopicsByMentorAsync(topicPoolId, mentorId, cancellationToken);
    }

    /// <summary>
    /// The per-mentor active-topic cap for a pool. The admin "Số lượng đề tài tối đa / GVHD" setting
    /// (<see cref="SettingKeys.MaxTopicsPerMentor"/>) is authoritative; the pool's own
    /// <see cref="TopicPool.MaxActiveTopicsPerMentor"/> is the fallback when the setting is unset.
    /// </summary>
    private async Task<MentorCannotExceedMaxActiveTopicsRule> BuildMentorTopicLimitRuleAsync(
        Guid mentorId, TopicPool pool, CancellationToken cancellationToken)
    {
        var maxTopics = await _settings.GetIntAsync(
            SettingKeys.MaxTopicsPerMentor, pool.MaxActiveTopicsPerMentor, cancellationToken);
        var currentCount = await GetMentorActiveTopicCountAsync(mentorId, pool.Id, cancellationToken);
        return new MentorCannotExceedMaxActiveTopicsRule(currentCount, maxTopics);
    }

    /// <summary>Throws when the mentor already holds the max number of active topics for the pool.</summary>
    private async Task EnsureMentorUnderTopicLimitAsync(Guid mentorId, TopicPool pool, CancellationToken cancellationToken)
    {
        var rule = await BuildMentorTopicLimitRuleAsync(mentorId, pool, cancellationToken);
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);
    }

    public async Task<RegisterFormProposalResult> ValidateRegisterFormAsync(
        Guid poolId, byte[] registerForm, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var (_, result, _) = await ParseValidatePoolFormAsync(poolId, registerForm, currentUserId, cancellationToken);
        return result;
    }

    public async Task<(Guid ProjectId, RegisterFormProposalResult Content)> ProposeTopicFromFormAsync(
        Guid poolId, byte[] registerForm, string? mentorNote, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var (pool, result, targetSemesterId) = await ParseValidatePoolFormAsync(poolId, registerForm, currentUserId, cancellationToken);

        // Cap the mentor's active topics per the admin "Số lượng đề tài tối đa / GVHD" setting. The
        // proposing lecturer (currentUserId) was already proven to be the mentor named on the form.
        await EnsureMentorUnderTopicLimitAsync(currentUserId, pool, cancellationToken);

        // Recorded for the pool-expiry audit trail only; null between semesters, which is fine.
        var createdInSemesterId = await _semesterDomainService.GetActiveSemesterIdAsync(cancellationToken);
        var code = await _projectsDomainService.GenerateProjectCodeAsync(targetSemesterId, pool.MajorId, cancellationToken);

        var expirationOffset = Math.Max(1, pool.ExpirationSemesters);
        var expirationSemesterId = await _semesterDomainService.GetSemesterAfterAsync(targetSemesterId, expirationOffset - 1, cancellationToken);

        var project = Project.CreateFromPool(
            code: code,
            nameVi: ProjectName.Create(result.NameVi),
            nameEn: ProjectName.Create(result.NameEn),
            nameAbbr: result.NameAbbr,
            description: result.Description,
            objectives: result.Objectives,
            scope: result.Scope,
            technologyStack: string.IsNullOrWhiteSpace(result.Technologies) ? null : TechnologyStack.Create(result.Technologies),
            expectedResults: result.ExpectedResults,
            majorId: pool.MajorId,
            semesterId: targetSemesterId,
            maxStudents: DefaultMaxStudents,
            topicPoolId: pool.Id,
            createdInSemesterId: createdInSemesterId,
            expirationSemesterId: expirationSemesterId);

        // Mentor = the logged-in lecturer, already validated to be the mentor named on the form.
        foreach (var mentorId in result.MentorIds)
            project.AddMentor(mentorId, assignedBy: mentorId);

        project.SetMentorNote(mentorNote);

        var roster = await ResolveRosterAsync(registerForm, project.MaxStudents, cancellationToken);
        if (roster.Count > 0)
            project.SetProposedRoster(roster);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (project.Id, result);
    }

    /// <summary>
    /// Loads the pool, parses the uploaded form, and runs the b → a → c validation + mapping — shared by
    /// the "validate before submit" step and the actual proposal so both apply exactly the same rules.
    /// </summary>
    private async Task<(TopicPool Pool, RegisterFormProposalResult Result, int TargetSemesterId)> ParseValidatePoolFormAsync(
        Guid poolId, byte[] registerForm, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (registerForm is null || registerForm.Length == 0)
            throw new BusinessRuleValidationException("Phiếu đăng ký (Capstone Project Register) là bắt buộc khi đề xuất đề tài.");

        var pool = await _topicPoolRepository.GetByIdAsync(poolId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(TopicPool), poolId);

        if (!pool.IsAcceptingProposals())
            throw new BusinessRuleValidationException("This topic pool is not currently accepting new topic proposals.");

        var targetSemesterId = await _semesterDomainService.GetRegistrationTargetSemesterIdAsync(cancellationToken)
            ?? throw new BusinessRuleValidationException(
                "Chưa có học kỳ nào đang mở đăng ký đề tài. Vui lòng liên hệ quản trị viên tạo học kỳ kế tiếp trước khi đề xuất đề tài.");

        RegisterFormContent content;
        using (var stream = new MemoryStream(registerForm, writable: false))
            content = _registerFormParser.ExtractContent(stream);

        var eligibleMentors = await _semesterRepository.GetEligibleMentorsByMajorAsync(targetSemesterId, pool.MajorId, cancellationToken);

        var result = RegisterFormProposalValidator.ValidateAndMap(content, eligibleMentors, currentUserId);
        return (pool, result, targetSemesterId);
    }

    /// <summary>
    /// Turns the attached register form into student ids. Rows that cannot be matched to a user are
    /// skipped rather than rejected — the roster is an optional convenience, not a gate on proposing.
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

    private async Task<User?> FindStudentAsync(RegisterRosterRow row, CancellationToken cancellationToken)
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
