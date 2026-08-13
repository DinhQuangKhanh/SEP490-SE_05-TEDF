using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Users.Commands.RevokeDepartmentHead;

/// <summary>
/// Handles <see cref="RevokeDepartmentHeadCommand"/> by delegating to <see cref="IUsersDomainService"/>.
/// </summary>
public class RevokeDepartmentHeadCommandHandler : ICommandHandler<RevokeDepartmentHeadCommand>
{
    private readonly IUsersDomainService _users;

    public RevokeDepartmentHeadCommandHandler(IUsersDomainService users) => _users = users;

    public async Task<Unit> Handle(RevokeDepartmentHeadCommand request, CancellationToken cancellationToken)
    {
        await _users.RevokeDepartmentHeadAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
