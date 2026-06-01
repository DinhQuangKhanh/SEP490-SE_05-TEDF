using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicRegistrationRejectedEventHandler : INotificationHandler<TopicRegistrationRejectedEvent>
    {
        private readonly ILogger<TopicRegistrationRejectedEventHandler> _logger;

        public TopicRegistrationRejectedEventHandler(ILogger<TopicRegistrationRejectedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicRegistrationRejectedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration rejected: {RegistrationId}, Project: {ProjectId}, Reason: {Reason}",
                notification.RegistrationId, notification.ProjectId, notification.Reason);
            // TODO: Notify group about rejection
            return Task.CompletedTask;
        }
    }
}
