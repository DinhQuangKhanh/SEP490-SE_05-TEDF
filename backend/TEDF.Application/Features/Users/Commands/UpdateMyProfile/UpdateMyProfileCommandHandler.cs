using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Services;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandHandler : ICommandHandler<UpdateMyProfileCommand>
{
    private readonly IUsersDomainService _users;
    private readonly ICurrentUserService _currentUser;

    public UpdateMyProfileCommandHandler(
        IUsersDomainService users,
        ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<MediatR.Unit> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        await _users.UpdateMyProfileAsync(
            userId,
            request.PhoneNumber,
            request.BirthDate,
            request.PrivacySettings,
            cancellationToken);

        return MediatR.Unit.Value;
    }
}
