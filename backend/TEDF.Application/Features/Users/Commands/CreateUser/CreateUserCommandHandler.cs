using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;

namespace TEDF.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUsersDomainService _usersDomainService;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(IUsersDomainService usersDomainService, ICurrentUserService currentUser)
    {
        _usersDomainService = usersDomainService;
        _currentUser = currentUser;
    }

    public Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var actingUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return _usersDomainService.CreateAsync(
            new CreateUserInput(
                request.Role, request.Email, request.FullName, request.Code,
                request.Phone, request.AcademicTitle, request.MajorId),
            actingUserId,
            cancellationToken);
    }
}
