using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;

public class RespondInvitationCommandHandler : ICommandHandler<RespondInvitationCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RespondInvitationCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RespondInvitationCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var group = await _groupRepository.GetWithInvitationsAsync(request.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), request.GroupId);

        var invitation = group.Invitations.FirstOrDefault(i => i.Id == request.InvitationId)
            ?? throw new EntityNotFoundException("GroupInvitation", request.InvitationId);

        if (request.Accept)
        {
            // Check if student is already in another active group
            if (await _groupRepository.IsStudentInActiveGroupAsync(studentId, group.SemesterId, cancellationToken))
                throw new BusinessRuleValidationException("Bạn đã có nhóm hoạt động trong học kỳ này.");

            group.AcceptInvitation(request.InvitationId, studentId);
        }
        else
        {
            group.RejectInvitation(request.InvitationId, studentId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
