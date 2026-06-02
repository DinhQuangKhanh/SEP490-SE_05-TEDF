using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetMentorGroups;

public class GetMentorGroupsQueryHandler : IQueryHandler<GetMentorGroupsQuery, List<MentorGroupDto>>
{
    private readonly IStudentGroupQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetMentorGroupsQueryHandler(IStudentGroupQueryService queryService, ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<List<MentorGroupDto>> Handle(GetMentorGroupsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var mentorId = _currentUser.UserId.Value;

        return await _queryService.GetMentorGroupsAsync(
            mentorId, request.SemesterId, cancellationToken);
    }
}
