using TEDF.Domain.Common.Rules;

namespace TEDF.Domain.Aggregates.ProjectAggregate.Rules
{
    /// <summary>
    /// A mentor can guide at most 4 groups per semester.
    /// </summary>
    public class MentorCannotExceedMaxGroupsPerSemesterRule : IBusinessRule
    {
        private readonly int _currentGroupCount;
        public const int MaxGroupsPerSemester = 4;

        public MentorCannotExceedMaxGroupsPerSemesterRule(int currentGroupCount)
        {
            _currentGroupCount = currentGroupCount;
        }

        public string Message => $"Giảng viên không thể hướng dẫn quá {MaxGroupsPerSemester} nhóm mỗi học kỳ. Hiện tại: {_currentGroupCount}/{MaxGroupsPerSemester}.";

        public bool IsBroken() => _currentGroupCount >= MaxGroupsPerSemester;
    }
}
