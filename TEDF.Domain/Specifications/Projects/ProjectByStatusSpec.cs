using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Specifications.Projects
{
    public class ProjectByStatusSpec : BaseSpecification<Project>
    {
        public ProjectByStatusSpec(ProjectStatus status) : base(p => p.Status == status)
        {
            ApplyOrderByDescending(p => p.CreatedAt);
        }
    }
}