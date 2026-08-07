using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.SetDepartmentHead;

/// <summary>
/// Handles <see cref="SetDepartmentHeadCommand"/> by delegating to <see cref="IUsersDomainService"/>.
/// Validation, the role transfer and persistence live in the domain service.
/// </summary>
public class SetDepartmentHeadCommandHandler : ICommandHandler<SetDepartmentHeadCommand>
{
    private readonly IUsersDomainService _users;
    private readonly ICurrentUserService _currentUser;

    public SetDepartmentHeadCommandHandler(IUsersDomainService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(SetDepartmentHeadCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _users.SetDepartmentHeadAsync(request.UserId, _currentUser.UserId.Value, cancellationToken);
        return Unit.Value;
    }
}
