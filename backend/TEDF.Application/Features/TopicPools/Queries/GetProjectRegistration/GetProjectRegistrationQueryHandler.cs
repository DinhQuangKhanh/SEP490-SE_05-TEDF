using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetProjectRegistration;

/// <summary>
/// Handles <see cref="GetProjectRegistrationQuery"/> by delegating to the read-side query service.
/// </summary>
public class GetProjectRegistrationQueryHandler
    : IQueryHandler<GetProjectRegistrationQuery, GroupRegistrationDto?>
{
    private readonly ITopicPoolsQueryService _queryService;

    public GetProjectRegistrationQueryHandler(ITopicPoolsQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<GroupRegistrationDto?> Handle(
        GetProjectRegistrationQuery request,
        CancellationToken cancellationToken)
        => _queryService.GetProjectRegistrationAsync(request.ProjectId, cancellationToken);
}
