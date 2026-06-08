using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.AssignDepartmentHead;

/// <summary>
/// Handles the AssignDepartmentHeadCommand by delegating to <see cref="IUsersDomainService"/>.
/// Validation, role swap and persistence live in the domain service.
/// Firebase custom claims are synced automatically via domain event handlers.
/// </summary>
public class AssignDepartmentHeadCommandHandler : ICommandHandler<AssignDepartmentHeadCommand>
{
    private readonly IUsersDomainService _users;
    private readonly ICurrentUserService _currentUser;

    public AssignDepartmentHeadCommandHandler(IUsersDomainService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(AssignDepartmentHeadCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        await _users.AssignDepartmentHeadAsync(
            request.DepartmentId, request.UserId, _currentUser.UserId.Value, cancellationToken);
        return Unit.Value;
    }
}
