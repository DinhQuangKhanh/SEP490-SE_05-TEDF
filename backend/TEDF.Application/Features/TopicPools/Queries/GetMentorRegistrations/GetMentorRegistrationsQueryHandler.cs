using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetMentorRegistrations;

/// <summary>
/// Returns the pending registration requests for the current mentor's pool topics.
/// </summary>
public class GetMentorRegistrationsQueryHandler
    : IQueryHandler<GetMentorRegistrationsQuery, List<MentorRegistrationRequestDto>>
{
    private readonly ITopicPoolsQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetMentorRegistrationsQueryHandler(
        ITopicPoolsQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<List<MentorRegistrationRequestDto>> Handle(
        GetMentorRegistrationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetMentorRegistrationsAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
