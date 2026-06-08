using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetOpenGroups;

public class GetOpenGroupsQueryHandler : IQueryHandler<GetOpenGroupsQuery, List<OpenGroupDto>>
{
    private readonly IStudentGroupsQueryService _queryService;
    private readonly ICurrentUserService _currentUserService;

    public GetOpenGroupsQueryHandler(
        IStudentGroupsQueryService queryService,
        ICurrentUserService currentUserService)
    {
        _queryService = queryService;
        _currentUserService = currentUserService;
    }

    public async Task<List<OpenGroupDto>> Handle(
        GetOpenGroupsQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetOpenGroupsAsync(studentId, request.SemesterId, cancellationToken);
    }
}
