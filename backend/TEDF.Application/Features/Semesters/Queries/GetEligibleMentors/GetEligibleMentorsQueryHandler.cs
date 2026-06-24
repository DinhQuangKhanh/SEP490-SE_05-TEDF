using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Features.Semesters.Queries.GetEligibleMentors;

public class GetEligibleMentorsQueryHandler : IQueryHandler<GetEligibleMentorsQuery, List<EligibleMentorDto>>
{
    private readonly ISemestersQueryService _semesters;

    public GetEligibleMentorsQueryHandler(ISemestersQueryService semesters) => _semesters = semesters;

    public Task<List<EligibleMentorDto>> Handle(GetEligibleMentorsQuery request, CancellationToken cancellationToken)
        => _semesters.GetEligibleMentorsAsync(request.SemesterId, cancellationToken);
}
