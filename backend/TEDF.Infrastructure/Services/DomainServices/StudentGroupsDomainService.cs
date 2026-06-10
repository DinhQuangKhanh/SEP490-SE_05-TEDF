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

    // ── Helper queries ──
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
        => await _groupRepository.GetActiveGroupIdsWithoutProjectAsync(semesterId, ct);

    // ── Write operations ──
    public async Task<Guid> CreateGroupAsync(Guid studentId, string? name, CancellationToken ct = default)
    {
        var nextSemester = await _semesterRepository.GetNextSemesterAsync(null, ct)
            ?? throw new BusinessRuleValidationException("No next semester found.");

        if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, nextSemester.Id, ct))
            throw new BusinessRuleValidationException("Student is already in an active group next semester.");

        if (await _groupRepository.HasPendingJoinRequestAsync(studentId, nextSemester.Id, ct))
            throw new BusinessRuleValidationException("Student has a pending join request and cannot create a new group yet.");

        var year = DateTime.UtcNow.Year;
        var seq = await _groupRepository.GetNextSequenceAsync(year, ct);
        var code = GroupCode.Generate(year, seq);

        var maxMembers = await _settings.GetIntAsync(SettingKeys.MaxGroupMembers, 5, ct);
        var group = Group.Create(code, nextSemester.Id, studentId, name, maxMembers);

        await _groupRepository.AddAsync(group, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
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
        var group = await _groupRepository.GetWithJoinRequestsAsync(groupId, ct)
            ?? throw new EntityNotFoundException(nameof(Group), groupId);

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

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName)
        => ex.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true
           || ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);
}
