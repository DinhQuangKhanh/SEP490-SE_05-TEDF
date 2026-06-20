using TEDF.Domain.Common.Interfaces;

namespace TEDF.Domain.Aggregates.SemesterAggregate.Events
{
    /// <summary>
    /// Raised when the admin finalizes a semester's eligibility roster. Handlers notify the
    /// assigned/unassigned mentors in-app and enqueue the eligible-student email job.
    /// </summary>
    public sealed record SemesterRosterPublishedEvent(int SemesterId, Guid PublishedBy) : DomainEventBase;
}
