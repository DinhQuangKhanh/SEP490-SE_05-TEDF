using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetEligibleStudents;

public class GetEligibleStudentsQueryHandler : IQueryHandler<GetEligibleStudentsQuery, List<EligibleStudentDto>>
{
    private readonly ISemestersQueryService _semesters;

    public GetEligibleStudentsQueryHandler(ISemestersQueryService semesters) => _semesters = semesters;

    public Task<List<EligibleStudentDto>> Handle(GetEligibleStudentsQuery request, CancellationToken cancellationToken)
        => _semesters.GetEligibleStudentsAsync(request.SemesterId, cancellationToken);
}
