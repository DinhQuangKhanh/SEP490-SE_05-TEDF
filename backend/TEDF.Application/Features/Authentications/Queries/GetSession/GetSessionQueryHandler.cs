using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Authentications.DTOs;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Application.Features.Authentications.Queries.GetSession;

public class GetSessionQueryHandler : IQueryHandler<GetSessionQuery, SessionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IAccessControlService _accessControl;

    public GetSessionQueryHandler(
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IAccessControlService accessControl)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _accessControl = accessControl;
    }

    public async Task<SessionDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        var access = await _accessControl.EvaluateAsync(userId, cancellationToken);

        return new SessionDto(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.GetActiveRoles().ToList(),
            user.Status.ToString(),
            new SessionAccessDto(access.Allowed, access.Kind, access.Reason));
    }
}
