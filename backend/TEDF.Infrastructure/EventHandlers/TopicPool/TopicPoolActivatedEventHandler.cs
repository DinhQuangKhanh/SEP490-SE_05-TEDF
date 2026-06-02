using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicPoolActivatedEventHandler : INotificationHandler<TopicPoolActivatedEvent>
    {
        private readonly ILogger<TopicPoolActivatedEventHandler> _logger;

        public TopicPoolActivatedEventHandler(ILogger<TopicPoolActivatedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicPoolActivatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic pool activated: {PoolId}", notification.TopicPoolId);
            return Task.CompletedTask;
        }
    }
}
