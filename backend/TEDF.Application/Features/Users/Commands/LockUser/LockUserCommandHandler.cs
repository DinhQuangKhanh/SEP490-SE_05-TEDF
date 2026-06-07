using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.LockUser;

public class LockUserCommandHandler : ICommandHandler<LockUserCommand>
{
    private readonly IUsersDomainService _users;
    private readonly ICurrentUserService _currentUser;

    public LockUserCommandHandler(IUsersDomainService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _users.LockAsync(request.UserId, _currentUser.UserId.Value, cancellationToken);
        return Unit.Value;
    }
}
