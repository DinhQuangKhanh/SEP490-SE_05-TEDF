using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Specifications.TopicRegistrations;

/// <summary>
/// Specification to get registrations by group.
/// </summary>
public class RegistrationsByGroupSpec : BaseSpecification<TopicRegistration>
{
    /// <summary>
    /// Gets all registrations for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="status">Optional: Filter by status</param>
    public RegistrationsByGroupSpec(Guid groupId, TopicRegistrationStatus? status = null)
        : base(r => r.GroupId == groupId &&
                    (!status.HasValue || r.Status == status.Value))
    {
        ApplyOrderByDescending(r => r.RegisteredAt);
    }
}
