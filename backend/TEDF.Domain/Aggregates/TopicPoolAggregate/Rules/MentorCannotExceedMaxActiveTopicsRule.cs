using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.TopicPoolAggregate.Rules
{
    /// <summary>
    /// Rule: Mentor cannot exceed maximum active topics in a pool.
    /// </summary>
    public class MentorCannotExceedMaxActiveTopicsRule : IBusinessRule
    {
        private readonly int _currentActiveTopicCount;
        private readonly int _maxActiveTopicsPerMentor;

        public MentorCannotExceedMaxActiveTopicsRule(int currentActiveTopicCount, int maxActiveTopicsPerMentor)
        {
            _currentActiveTopicCount = currentActiveTopicCount;
            _maxActiveTopicsPerMentor = maxActiveTopicsPerMentor;
        }

        public bool IsBroken() => _currentActiveTopicCount >= _maxActiveTopicsPerMentor;

        public string Message => $"Mentor has already reached the maximum of {_maxActiveTopicsPerMentor} active topics in this pool.";
    }
}
