using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.Projects
{
    public class ProjectNeedsModificationSpec : BaseSpecification<Project>
    {
        public ProjectNeedsModificationSpec(Guid? mentorId = null)
            : base(p => p.Status == ProjectStatus.NeedsModification &&
                        (!mentorId.HasValue || p.Mentors.Any(m => m.MentorId == mentorId.Value && m.IsActive)))
        {
            AddInclude(p => p.Mentors);
            ApplyOrderBy(p => p.UpdatedAt!);
        }
    }
}
