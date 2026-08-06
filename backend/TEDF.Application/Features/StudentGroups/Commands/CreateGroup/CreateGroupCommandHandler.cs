using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.StudentGroups.Commands.CreateGroup;

public class CreateGroupCommandHandler : ICommandHandler<CreateGroupCommand, Guid>
{
    private readonly IStudentGroupsDomainService _groups;
    private readonly ICurrentUserService _currentUser;

    public CreateGroupCommandHandler(IStudentGroupsDomainService groups, ICurrentUserService currentUser)
    {
        _groups = groups;
        _currentUser = currentUser;
    }

    public Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return _groups.CreateGroupAsync(studentId, cancellationToken);
    }
}
