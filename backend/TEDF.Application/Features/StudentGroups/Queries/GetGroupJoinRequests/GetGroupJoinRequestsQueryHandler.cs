using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetGroupJoinRequests;

public class GetGroupJoinRequestsQueryHandler : IQueryHandler<GetGroupJoinRequestsQuery, List<JoinRequestDto>>
{
    private readonly IStudentGroupQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetGroupJoinRequestsQueryHandler(
        IStudentGroupQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<List<JoinRequestDto>> Handle(
        GetGroupJoinRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetGroupJoinRequestsAsync(
            request.GroupId, cancellationToken);
    }
}
