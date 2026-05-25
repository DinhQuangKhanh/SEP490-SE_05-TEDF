using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    public class ProjectCannotExceedMaxMentorsRule : IBusinessRule
    {
        private const int MaxMentors = 2;
        private readonly int _currentMentorCount;

        public ProjectCannotExceedMaxMentorsRule(int currentMentorCount)
        {
            _currentMentorCount = currentMentorCount;
        }

        public string Message => $"Project cannot have more than {MaxMentors} mentors.";

        public bool IsBroken() => _currentMentorCount >= MaxMentors;
    }
}
