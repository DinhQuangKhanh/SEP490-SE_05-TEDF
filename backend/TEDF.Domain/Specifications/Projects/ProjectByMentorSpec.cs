using TEDF.Domain.Aggregates.ProjectAggregate;

namespace TEDF.Domain.Specifications.Projects
{
    public class ProjectByMentorSpec : BaseSpecification<Project>
    {
        public ProjectByMentorSpec(Guid mentorId) : base(p => p.Mentors.Any(m => m.MentorId == mentorId && m.IsActive))
        {
            AddInclude(p => p.Mentors);
            ApplyOrderByDescending(p => p.CreatedAt);
        }
    }
}
