using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicPoolSuspendedEventHandler : INotificationHandler<TopicPoolSuspendedEvent>
    {
        private readonly ILogger<TopicPoolSuspendedEventHandler> _logger;

        public TopicPoolSuspendedEventHandler(ILogger<TopicPoolSuspendedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicPoolSuspendedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic pool suspended: {PoolId}", notification.TopicPoolId);
            return Task.CompletedTask;
        }
    }
}
