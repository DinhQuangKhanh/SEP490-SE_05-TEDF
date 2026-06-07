using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.InviteMember;

public class InviteMemberCommandHandler : ICommandHandler<InviteMemberCommand, int>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public InviteMemberCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public Task<int> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var inviterId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        return _groups.InviteMemberAsync(request.GroupId, inviterId, request.StudentCode, request.Message, cancellationToken);
    }
}
