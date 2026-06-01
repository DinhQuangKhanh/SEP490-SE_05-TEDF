using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Mentor.DTOs;

namespace TEDF.Application.Features.Mentor.Queries.GetMentorTopics;

public class GetMentorTopicsQueryHandler
    : IQueryHandler<GetMentorTopicsQuery, GetMentorTopicsResult>
{
    private readonly ITopicQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetMentorTopicsQueryHandler(
        ITopicQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<GetMentorTopicsResult> Handle(
        GetMentorTopicsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetMentorTopicsAsync(
            _currentUser.UserId.Value,
            request.SemesterId,
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
