using TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the DirectTopics feature. Query handlers depend on this only.
/// </summary>
public interface IDirectTopicsQueryService
{
    Task<AvailableMentorsResponse> GetAvailableMentorsAsync(Guid groupId, CancellationToken cancellationToken = default);
}
