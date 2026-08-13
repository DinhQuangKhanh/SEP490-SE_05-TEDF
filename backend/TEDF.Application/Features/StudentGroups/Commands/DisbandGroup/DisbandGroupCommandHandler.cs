using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.DisbandGroup;

public class DisbandGroupCommandHandler : ICommandHandler<DisbandGroupCommand>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public DisbandGroupCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DisbandGroupCommand request, CancellationToken cancellationToken)
    {
        var leaderId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");

        await _groups.DisbandGroupAsync(request.GroupId, leaderId, cancellationToken);
        return Unit.Value;
    }
}
