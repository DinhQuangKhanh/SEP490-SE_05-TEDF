using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Rules
{
    /// <summary>
    /// Rule: Group cannot have multiple pending registrations for the same topic.
    /// </summary>
    public class GroupCannotHaveDuplicatePendingRegistrationRule : IBusinessRule
    {
        private readonly bool _hasPendingRegistration;

        public GroupCannotHaveDuplicatePendingRegistrationRule(bool hasPendingRegistration)
        {
            _hasPendingRegistration = hasPendingRegistration;
        }

        public bool IsBroken() => _hasPendingRegistration;

        public string Message => "Group already has a pending registration for this topic.";
    }
}
