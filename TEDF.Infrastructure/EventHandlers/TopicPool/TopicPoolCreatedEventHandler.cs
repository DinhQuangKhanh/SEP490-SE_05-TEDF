using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicPoolCreatedEventHandler : INotificationHandler<TopicPoolCreatedEvent>
    {
        private readonly ILogger<TopicPoolCreatedEventHandler> _logger;

        public TopicPoolCreatedEventHandler(ILogger<TopicPoolCreatedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicPoolCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic pool created: {PoolId}, Code: {Code}, MajorId: {MajorId}",
                notification.TopicPoolId, notification.Code, notification.MajorId);
            return Task.CompletedTask;
        }
    }
}
