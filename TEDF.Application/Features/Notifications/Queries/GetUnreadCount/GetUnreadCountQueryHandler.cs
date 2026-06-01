using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Handles GetUnreadCountQuery by returning the number of unread notifications
/// for the current authenticated user.
/// </summary>
public class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, long>
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(
        INotificationService notificationService,
        ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    public async Task<long> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _notificationService.GetUnreadCountAsync(_currentUser.UserId.Value, request.Category, cancellationToken);
    }
}
