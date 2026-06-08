using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.StudentGroups.DTOs;

namespace TEDF.Application.Features.StudentGroups.Queries.GetInvitableStudents;

public class GetInvitableStudentsQueryHandler : IQueryHandler<GetInvitableStudentsQuery, List<AvailableStudentDto>>
{
    private readonly IStudentGroupsQueryService _queryService;

    public GetInvitableStudentsQueryHandler(IStudentGroupsQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<List<AvailableStudentDto>> Handle(GetInvitableStudentsQuery request, CancellationToken cancellationToken)
        => _queryService.GetInvitableStudentsAsync(request.GroupId, cancellationToken);
}
