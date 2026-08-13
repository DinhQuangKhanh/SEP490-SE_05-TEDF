using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.LeaveGroup;

public class LeaveGroupCommandHandler : ICommandHandler<LeaveGroupCommand>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public LeaveGroupCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        await _groups.LeaveGroupAsync(request.GroupId, studentId, cancellationToken);
        return Unit.Value;
    }
}
