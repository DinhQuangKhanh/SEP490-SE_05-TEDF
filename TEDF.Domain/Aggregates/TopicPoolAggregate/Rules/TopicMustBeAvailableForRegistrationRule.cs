using TEDF.Domain.Common.Rules;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Rules
{
    /// <summary>
    /// Rule: Topic must be available for registration (not expired, not already assigned).
    /// </summary>
    public class TopicMustBeAvailableForRegistrationRule : IBusinessRule
    {
        private readonly PoolTopicStatus _poolStatus;

        public TopicMustBeAvailableForRegistrationRule(PoolTopicStatus poolStatus)
        {
            _poolStatus = poolStatus;
        }

        public bool IsBroken() => _poolStatus != PoolTopicStatus.Available;

        public string Message => _poolStatus switch
        {
            PoolTopicStatus.Reserved => "Topic is currently reserved by another group.",
            PoolTopicStatus.Assigned => "Topic has already been assigned to a group.",
            PoolTopicStatus.Expired => "Topic has expired and is no longer available.",
            _ => "Topic is not available for registration."
        };
    }
}
