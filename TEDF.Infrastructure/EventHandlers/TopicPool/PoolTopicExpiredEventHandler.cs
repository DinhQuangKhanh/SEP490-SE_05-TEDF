using MediatR;
using Microsoft.Extensions.Logging;
using TEDF.Domain.Aggregates.TopicPoolAggregate.Events;

namespace TEDF.Infrastructure.EventHandlers.TopicPool
{
    public class PoolTopicExpiredEventHandler : INotificationHandler<PoolTopicExpiredEvent>
    {
        private readonly ILogger<PoolTopicExpiredEventHandler> _logger;

        public PoolTopicExpiredEventHandler(ILogger<PoolTopicExpiredEventHandler> logger) => _logger = logger;

        public Task Handle(PoolTopicExpiredEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Pool topic expired: {ProjectId}, Pool: {PoolId}, Created: {CreatedSemester}, Expired: {ExpiredSemester}",
                notification.ProjectId, notification.TopicPoolId, notification.CreatedSemesterId, notification.ExpiredAtSemesterId);
            // TODO: Notify mentor about expired topic
            return Task.CompletedTask;
        }
    }
}
