using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class TopicRegistrationConfirmedEventHandler : INotificationHandler<TopicRegistrationConfirmedEvent>
    {
        private readonly ILogger<TopicRegistrationConfirmedEventHandler> _logger;

        public TopicRegistrationConfirmedEventHandler(ILogger<TopicRegistrationConfirmedEventHandler> logger) => _logger = logger;

        public Task Handle(TopicRegistrationConfirmedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Topic registration confirmed: {RegistrationId}, Project: {ProjectId}, Group: {GroupId}",
                notification.RegistrationId, notification.ProjectId, notification.GroupId);
            // TODO: Notify group about confirmation
            return Task.CompletedTask;
        }
    }
}
