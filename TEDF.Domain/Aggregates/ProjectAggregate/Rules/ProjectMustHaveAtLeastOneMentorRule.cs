using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    /// <summary>
    /// Business rule that ensures a project always has at least one active mentor.
    /// Prevents removing the last mentor from a project.
    /// </summary>
    public class ProjectMustHaveAtLeastOneMentorRule : IBusinessRule
    {
        private readonly int _activeMentorCountAfterRemoval;

        public ProjectMustHaveAtLeastOneMentorRule(int activeMentorCountAfterRemoval)
        {
            _activeMentorCountAfterRemoval = activeMentorCountAfterRemoval;
        }

        public string Message => "Mỗi đề tài phải có ít nhất 1 mentor. Không thể xóa mentor cuối cùng.";

        public bool IsBroken() => _activeMentorCountAfterRemoval < 1;
    }
}
