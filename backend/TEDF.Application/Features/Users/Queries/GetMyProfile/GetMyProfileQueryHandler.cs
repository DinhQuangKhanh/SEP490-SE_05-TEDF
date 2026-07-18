using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Users.DTOs;

namespace TEDF.Application.Features.Users.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, MyProfileDto>
{
    private readonly IUsersQueryService _users;
    private readonly ICurrentUserService _currentUser;

    public GetMyProfileQueryHandler(IUsersQueryService users, ICurrentUserService currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return _users.GetMyProfileAsync(userId, cancellationToken);
    }
}
