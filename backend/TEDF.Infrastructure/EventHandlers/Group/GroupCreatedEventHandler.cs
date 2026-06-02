using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.GroupAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.Group
{
    public class GroupCreatedEventHandler : INotificationHandler<GroupCreatedEvent>
    {
        private readonly ILogger<GroupCreatedEventHandler> _logger;

        public GroupCreatedEventHandler(ILogger<GroupCreatedEventHandler> logger) => _logger = logger;

        public Task Handle(GroupCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Group created: {GroupId}, Code: {GroupCode}", notification.GroupId, notification.GroupCode);
            return Task.CompletedTask;
        }
    }
}
