using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicRegistrationRequestedEventHandler : INotificationHandler<TopicRegistrationRequestedEvent>
    {
        private readonly ILogger<TopicRegistrationRequestedEventHandler> _logger;

        public TopicRegistrationRequestedEventHandler(ILogger<TopicRegistrationRequestedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicRegistrationRequestedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration requested: {RegistrationId}, Project: {ProjectId}, Group: {GroupId}",
                notification.RegistrationId, notification.ProjectId, notification.GroupId);
            // TODO: Notify mentor about pending registration
            return Task.CompletedTask;
        }
    }
}
