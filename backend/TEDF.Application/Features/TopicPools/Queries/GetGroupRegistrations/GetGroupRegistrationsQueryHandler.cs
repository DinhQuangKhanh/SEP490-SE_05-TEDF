using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetGroupRegistrations;

/// <summary>
/// Handles <see cref="GetGroupRegistrationsQuery"/> by delegating to the read-side query service.
/// </summary>
public class GetGroupRegistrationsQueryHandler
    : IQueryHandler<GetGroupRegistrationsQuery, List<GroupRegistrationDto>>
{
    private readonly ITopicPoolsQueryService _queryService;

    public GetGroupRegistrationsQueryHandler(ITopicPoolsQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<List<GroupRegistrationDto>> Handle(
        GetGroupRegistrationsQuery request,
        CancellationToken cancellationToken)
        => _queryService.GetGroupRegistrationsAsync(request.GroupId, cancellationToken);
}
