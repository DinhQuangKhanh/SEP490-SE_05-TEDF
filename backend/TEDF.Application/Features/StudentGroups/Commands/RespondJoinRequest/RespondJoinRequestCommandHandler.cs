using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondJoinRequest;

public class RespondJoinRequestCommandHandler : ICommandHandler<RespondJoinRequestCommand>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public RespondJoinRequestCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RespondJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var leaderId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        await _groups.RespondJoinRequestAsync(request.GroupId, request.RequestId, leaderId, request.Approve, cancellationToken);
        return Unit.Value;
    }
}
