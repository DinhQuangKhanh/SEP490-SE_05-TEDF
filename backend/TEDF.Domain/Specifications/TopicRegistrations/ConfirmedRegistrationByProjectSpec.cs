using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Specifications.TopicRegistrations;

/// <summary>
/// Specification to get the confirmed registration for a project.
/// Each project can have at most one confirmed registration.
/// </summary>
public class ConfirmedRegistrationByProjectSpec : BaseSpecification<TopicRegistration>
{
    /// <summary>
    /// Gets the confirmed registration for a specific project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    public ConfirmedRegistrationByProjectSpec(Guid projectId)
        : base(r => r.ProjectId == projectId && r.Status == TopicRegistrationStatus.Confirmed)
    {
    }
}
