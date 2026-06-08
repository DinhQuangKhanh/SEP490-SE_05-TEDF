using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Aggregates.UserAggregate.Events;
using TEDF.Infrastructure.Authentication;

namespace TEDF.Infrastructure.EventHandlers.User;

public class SyncFirebaseClaimsOnUserCreatedHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SyncFirebaseClaimsOnUserCreatedHandler> _logger;

    public SyncFirebaseClaimsOnUserCreatedHandler(
        IFirebaseAuthService firebaseAuth,
        IUserRepository userRepository,
        ILogger<SyncFirebaseClaimsOnUserCreatedHandler> logger)
    {
        _firebaseAuth = firebaseAuth;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("User {UserId} not found when syncing Firebase claims.", notification.UserId);
                return;
            }

            var claims = new Dictionary<string, object>
            {
                ["dbUserId"] = user.Id.ToString(),
                ["roles"] = user.GetActiveRoles().ToArray()
            };

            await _firebaseAuth.SetCustomClaimsAsync(notification.FirebaseUid, claims, cancellationToken);

            _logger.LogInformation(
                "Synced Firebase custom claims for new user {UserId} ({Email}): roles=[{Roles}]",
                user.Id, notification.Email, string.Join(", ", user.GetActiveRoles()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to sync Firebase custom claims for new user {UserId} ({FirebaseUid}). " +
                "Claims will be synced on next role assignment.",
                notification.UserId, notification.FirebaseUid);
        }
    }
}
