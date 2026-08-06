using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Aggregates.UserAggregate.Events;
using TEDF.Infrastructure.Authentication;

namespace TEDF.Infrastructure.EventHandlers.User;

public class SyncFirebaseClaimsOnRoleAssignedHandler : INotificationHandler<UserRoleAssignedEvent>
{
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<SyncFirebaseClaimsOnRoleAssignedHandler> _logger;

    public SyncFirebaseClaimsOnRoleAssignedHandler(
        IFirebaseAuthService firebaseAuth,
        IUserRepository userRepository,
        ILogger<SyncFirebaseClaimsOnRoleAssignedHandler> logger)
    {
        _firebaseAuth = firebaseAuth;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(UserRoleAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("User {UserId} not found when syncing Firebase claims on role assignment.", notification.UserId);
                return;
            }

            // "Pending" accounts created during roster import have no real Firebase user yet, so
            // SetCustomClaims would make a failing network round-trip per user (a big drag on bulk
            // imports). Claims are synced when the account is linked on first Google sign-in.
            if (user.IsPendingActivation)
                return;

            var activeRoles = user.GetActiveRoles().ToArray();

            var claims = new Dictionary<string, object>
            {
                ["dbUserId"] = user.Id.ToString(),
                ["roles"] = activeRoles
            };

            await _firebaseAuth.SetCustomClaimsAsync(user.FirebaseUid, claims, cancellationToken);

            _logger.LogInformation(
                "Synced Firebase custom claims for user {UserId} after role '{RoleName}' assigned: roles=[{Roles}]",
                user.Id, notification.RoleName, string.Join(", ", activeRoles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to sync Firebase custom claims for user {UserId} after role '{RoleName}' assignment.",
                notification.UserId, notification.RoleName);
        }
    }
}
