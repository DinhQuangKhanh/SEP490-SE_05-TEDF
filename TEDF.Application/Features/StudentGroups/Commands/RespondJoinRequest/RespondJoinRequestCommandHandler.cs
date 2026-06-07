using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;

public class RespondJoinRequestCommandHandler : ICommandHandler<RespondJoinRequestCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RespondJoinRequestCommandHandler(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RespondJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var leaderId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        var group = await _groupRepository.GetWithJoinRequestsAsync(request.GroupId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Group), request.GroupId);

        if (request.Approve)
        {
            var joinRequest = group.JoinRequests.FirstOrDefault(r => r.Id == request.RequestId)
                ?? throw new EntityNotFoundException("GroupJoinRequest", request.RequestId);

            if (await _groupRepository.IsStudentInActiveGroupAsync(joinRequest.StudentId, group.SemesterId, cancellationToken))
                throw new BusinessRuleValidationException("Sinh viên đã có nhóm hoạt động trong học kỳ này.");

            group.ApproveJoinRequest(request.RequestId, leaderId);
        }
        else
        {
            group.RejectJoinRequest(request.RequestId, leaderId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
