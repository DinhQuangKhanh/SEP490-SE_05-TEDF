using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, long>
{
    private readonly INotificationsQueryService _notifications;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(INotificationsQueryService notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public Task<long> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _notifications.GetUnreadCountAsync(_currentUser.UserId.Value, request.Category, cancellationToken);
    }
}
