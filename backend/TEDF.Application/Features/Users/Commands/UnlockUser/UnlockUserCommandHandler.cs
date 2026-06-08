using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.UnlockUser;

public class UnlockUserCommandHandler : ICommandHandler<UnlockUserCommand>
{
    private readonly IUsersDomainService _users;
    private readonly ICurrentUserService _currentUser;

    public UnlockUserCommandHandler(IUsersDomainService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _users.UnlockAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
