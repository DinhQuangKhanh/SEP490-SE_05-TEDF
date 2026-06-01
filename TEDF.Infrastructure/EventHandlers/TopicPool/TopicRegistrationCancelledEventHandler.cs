using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicRegistrationCancelledEventHandler : INotificationHandler<TopicRegistrationCancelledEvent>
    {
        private readonly ILogger<TopicRegistrationCancelledEventHandler> _logger;

        public TopicRegistrationCancelledEventHandler(ILogger<TopicRegistrationCancelledEventHandler> logger) => _logger = logger;

        public Task Handle(TopicRegistrationCancelledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration cancelled: {RegistrationId}, Project: {ProjectId}, Reason: {Reason}",
                notification.RegistrationId, notification.ProjectId, notification.Reason);
            return Task.CompletedTask;
        }
    }
}
