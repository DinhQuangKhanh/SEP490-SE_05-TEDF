using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.GroupAggregate.ValueObjects;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Services;
using ISystemSettingsService = TEDF.Application.Common.Interfaces.ISystemSettingsService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the StudentGroups feature. See <see cref="IStudentGroupsDomainService"/>.
/// </summary>
public class StudentGroupsDomainService : IStudentGroupsDomainService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemSettingsService _settings;
    private readonly IUnitOfWork _unitOfWork;

    public StudentGroupsDomainService(
        IGroupRepository groupRepository,
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        IUserRepository userRepository,
        ISystemSettingsService settings,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    // ── Write operations ──
    // Parameter is spelled out to match IStudentGroupsDomainService; the rest of this file still
    // uses the shorter `ct`, which predates that convention.
    public async Task<Guid> CreateGroupAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        // The group belongs to the semester this student was approved for — not to "the semester
        // after the one running right now". Those two differ whenever the newly created semester is
        // itself the running one, or whenever today falls in the gap between two semesters; in both
        // cases the old lookup returned null and students were told "No next semester found" even
        // though their roster entry was in place.
        var nextSemester = await _semesterRepository.GetEligibleSemesterForStudentAsync(studentId, cancellationToken)
            ?? throw new BusinessRuleValidationException(
                "Bạn không thuộc danh sách sinh viên đủ điều kiện của học kỳ hiện tại hoặc sắp tới.");

        if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, nextSemester.Id, cancellationToken))
            throw new BusinessRuleValidationException("Student is already in an active group next semester.");

        if (await _groupRepository.HasPendingJoinRequestAsync(studentId, nextSemester.Id, cancellationToken))
            throw new BusinessRuleValidationException("Student has a pending join request and cannot create a new group yet.");

        // Numbering is per semester, so the code prefix and the sequence must come from the same
        // semester the group is being created in — not from the calendar year.
        var seq = await _groupRepository.GetNextSequenceAsync(nextSemester.Id, cancellationToken);
        var code = GroupCode.Generate(nextSemester.Code.Value, seq);

        var maxMembers = await _settings.GetIntAsync(SettingKeys.MaxGroupMembers, 5, cancellationToken);
        var group = Group.Create(code, nextSemester.Id, studentId, maxMembers: maxMembers);

        await _groupRepository.AddAsync(group, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_Groups_Code"))
        {
            throw new ConcurrencyException(nameof(Group), code.Value);
        }

        return group.Id;
    }

    public async Task<int> InviteMemberAsync(Guid groupId, Guid inviterId, string studentCode, string? message, CancellationToken ct = default)
    {
        var group = await _groupRepository.GetWithInvitationsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        var invitee = await _userRepository.GetByStudentCodeAsync(studentCode, ct)
            ?? throw new EntityNotFoundException("User", studentCode);

        if (!await _semesterRepository.IsStudentEligibleAsync(invitee.Id, group.SemesterId, ct))
            throw new BusinessRuleValidationException("Sinh viên này không đủ điều kiện làm đồ án trong học kỳ này.");

        if (await _groupRepository.IsStudentInActiveGroupAsync(invitee.Id, group.SemesterId, ct))
            throw new BusinessRuleValidationException("Sinh viên này đã có nhóm hoạt động trong học kỳ này.");

        var invitation = group.InviteMember(inviterId, invitee.Id, message);

        var groupProject = await _projectRepository.GetByGroupIdAsync(groupId, ct);
        if (groupProject != null && groupProject.Status != ProjectStatus.Rejected && groupProject.Status != ProjectStatus.Cancelled && groupProject.Status != ProjectStatus.Draft)
            throw new BusinessRuleValidationException("Nhóm đã có đề tài và không thể mời thành viên mới.");

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_GroupInvitations_GroupId_InviteeId_Pending"))
        {
            throw new BusinessRuleValidationException("Sinh viên này đã có lời mời tham gia nhóm đang chờ xử lý.");
        }

        return invitation.Id;
    }

    public async Task<int> RequestJoinAsync(Guid groupId, Guid studentId, string? message, CancellationToken ct = default)
    {
        var group = await _groupRepository.GetWithJoinRequestsAndInvitationsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        // If the group already invited this student, they should respond to the invitation, not
        // send a competing join request.
        if (group.Invitations.Any(i => i.IsPending && i.InviteeId == studentId))
            throw new BusinessRuleValidationException(
                "Nhóm này đã gửi lời mời cho bạn. Vui lòng phản hồi lời mời thay vì gửi yêu cầu tham gia.");

        if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, group.SemesterId, ct))
            throw new BusinessRuleValidationException("Bạn đã có nhóm hoạt động trong học kỳ này.");

        if (await _groupRepository.HasPendingJoinRequestAsync(studentId, group.SemesterId, ct))
            throw new BusinessRuleValidationException("Bạn đã có một yêu cầu tham gia nhóm đang chờ xử lý trong học kỳ này.");

        var groupProject = await _projectRepository.GetByGroupIdAsync(groupId, ct);
        if (groupProject != null && groupProject.Status != ProjectStatus.Rejected && groupProject.Status != ProjectStatus.Cancelled && groupProject.Status != ProjectStatus.Draft)
            throw new BusinessRuleValidationException("Nhóm đã có đề tài và không thể thêm thành viên mới.");

        var joinRequest = group.RequestToJoin(studentId, message);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "IX_GroupJoinRequests_GroupId_StudentId_Pending"))
        {
            throw new BusinessRuleValidationException("Bạn đã có một yêu cầu tham gia nhóm đang chờ xử lý.");
        }

        return joinRequest.Id;
    }

    public async Task RespondInvitationAsync(Guid groupId, int invitationId, Guid studentId, bool accept, CancellationToken ct = default)
    {
        var group = await _groupRepository.GetWithInvitationsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        _ = group.Invitations.FirstOrDefault(i => i.Id == invitationId)
            ?? throw new EntityNotFoundException("GroupInvitation", invitationId);

        if (accept)
        {
            if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, group.SemesterId, ct))
                throw new BusinessRuleValidationException("Bạn đã có nhóm hoạt động trong học kỳ này.");

            group.AcceptInvitation(invitationId, studentId);
        }
        else
        {
            group.RejectInvitation(invitationId, studentId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RespondJoinRequestAsync(Guid groupId, int requestId, Guid leaderId, bool approve, CancellationToken ct = default)
    {
        var group = await _groupRepository.GetWithJoinRequestsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        if (approve)
        {
            var joinRequest = group.JoinRequests.FirstOrDefault(r => r.Id == requestId)
                ?? throw new EntityNotFoundException("GroupJoinRequest", requestId);

            if (await _groupRepository.IsStudentInActiveGroupAsync(joinRequest.StudentId, group.SemesterId, ct))
                throw new BusinessRuleValidationException("Sinh viên đã có nhóm hoạt động trong học kỳ này.");

            var groupProject = await _projectRepository.GetByGroupIdAsync(groupId, ct);
            if (groupProject != null && groupProject.Status != ProjectStatus.Rejected && groupProject.Status != ProjectStatus.Cancelled && groupProject.Status != ProjectStatus.Draft)
                throw new BusinessRuleValidationException("Nhóm đã có đề tài và không thể thêm thành viên mới.");

            group.ApproveJoinRequest(requestId, leaderId);
        }
        else
        {
            group.RejectJoinRequest(requestId, leaderId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task LeaveGroupAsync(Guid groupId, Guid studentId, CancellationToken ct = default)
    {
        var group = await _groupRepository.GetWithMembersAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        await EnsureGroupHasNoLiveProjectAsync(groupId, "Nhóm đã có đề tài, bạn không thể rời nhóm.", ct);

        group.LeaveGroup(studentId);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DisbandGroupAsync(Guid groupId, Guid leaderId, CancellationToken ct = default)
    {
        // Disbanding closes pending invitations and join requests too, so they must be loaded.
        var group = await _groupRepository.GetWithAllRelationsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

        await EnsureGroupHasNoLiveProjectAsync(groupId, "Nhóm đã có đề tài, không thể giải tán nhóm.", ct);

        group.Disband(leaderId);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Blocks membership changes once the group's topic is live. Draft/Rejected/Cancelled don't count —
    /// those are the same states that still allow inviting and joining.
    /// </summary>
    private async Task EnsureGroupHasNoLiveProjectAsync(Guid groupId, string message, CancellationToken ct)
    {
        var groupProject = await _projectRepository.GetByGroupIdAsync(groupId, ct);
        if (groupProject != null
            && groupProject.Status != ProjectStatus.Rejected
            && groupProject.Status != ProjectStatus.Cancelled
            && groupProject.Status != ProjectStatus.Draft)
        {
            throw new BusinessRuleValidationException(message);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName)
        => ex.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true
           || ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);
}
