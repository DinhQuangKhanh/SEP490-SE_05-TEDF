using TEDF.Domain.Aggregates.TopicPoolAggregate.Entities;

namespace TEDF.Domain.Services;

/// <summary>
/// Write-side service for the pool-registration lifecycle: a group requests a topic, and the mentor
/// confirms/rejects it or the group leader cancels it. Split out of the old god-service
/// <c>ITopicPoolsDomainService</c> (single responsibility: registrations).
/// </summary>
public interface ITopicRegistrationService
{
    /// <summary>Processes a topic registration request from a group.</summary>
    Task<TopicRegistration> RequestRegistrationAsync(
        Guid projectId,
        Guid groupId,
        Guid registeredBy,
        int priority = 1,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>Confirms a topic registration and assigns the group to the project.</summary>
    Task ConfirmRegistrationAsync(Guid registrationId, Guid confirmedBy, CancellationToken cancellationToken = default);

    /// <summary>Rejects a topic registration.</summary>
    Task RejectRegistrationAsync(Guid registrationId, Guid rejectedBy, string reason, CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending topic registration. Only the leader of the registering group may cancel.</summary>
    Task CancelRegistrationAsync(Guid registrationId, Guid cancelledBy, string? reason, CancellationToken cancellationToken = default);
}
