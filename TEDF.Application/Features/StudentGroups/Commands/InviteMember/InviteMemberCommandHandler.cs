using TEDF.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.InviteMember;

public class InviteMemberCommandHandler : ICommandHandler<InviteMemberCommand, int>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public InviteMemberCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var inviterId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var group = await _groupRepository.GetWithInvitationsAsync(request.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), request.GroupId);

        // Find the student by code
        var invitee = await _userRepository.GetByStudentCodeAsync(request.StudentCode, cancellationToken)
            ?? throw new EntityNotFoundException("User", request.StudentCode);

        // Check if invitee is already in an active group this semester
        if (await _groupRepository.IsStudentInActiveGroupAsync(invitee.Id, group.SemesterId, cancellationToken))
            throw new BusinessRuleValidationException("Sinh viên này đã có nhóm hoạt động trong học kỳ này.");

        // Domain logic validates leader, capacity, duplicates
        var invitation = group.InviteMember(inviterId, invitee.Id, request.Message);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPendingInvitationUniqueViolation(ex))
        {
            throw new BusinessRuleValidationException("Sinh viên này đã có lời mời tham gia nhóm đang chờ xử lý.");
        }

        return invitation.Id;
    }

    private static bool IsPendingInvitationUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_GroupInvitations_GroupId_InviteeId_Pending", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("IX_GroupInvitations_GroupId_InviteeId_Pending", StringComparison.OrdinalIgnoreCase);
    }
}
