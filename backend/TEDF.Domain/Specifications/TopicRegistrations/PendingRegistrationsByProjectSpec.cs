using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Specifications.TopicRegistrations;

/// <summary>
/// Specification to get pending registrations for a project/topic.
/// </summary>
public class PendingRegistrationsByProjectSpec : BaseSpecification<TopicRegistration>
{
    /// <summary>
    /// Gets all pending registrations for a specific project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    public PendingRegistrationsByProjectSpec(Guid projectId)
        : base(r => r.ProjectId == projectId && r.Status == TopicRegistrationStatus.Pending)
    {
        ApplyOrderBy(r => r.Priority);
        ApplyThenBy(r => r.RegisteredAt);
    }
}
