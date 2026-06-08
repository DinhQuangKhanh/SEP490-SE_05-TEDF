using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.RespondInvitation;

public class RespondInvitationCommandHandler : ICommandHandler<RespondInvitationCommand>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public RespondInvitationCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RespondInvitationCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        await _groups.RespondInvitationAsync(request.GroupId, request.InvitationId, studentId, request.Accept, cancellationToken);
        return Unit.Value;
    }
}
