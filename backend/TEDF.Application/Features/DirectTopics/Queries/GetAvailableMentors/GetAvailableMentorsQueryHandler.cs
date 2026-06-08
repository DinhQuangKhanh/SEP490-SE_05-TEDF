using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

public class GetAvailableMentorsQueryHandler : IQueryHandler<GetAvailableMentorsQuery, List<AvailableMentorDto>>
{
    private readonly IDirectTopicsQueryService _directTopics;

    public GetAvailableMentorsQueryHandler(IDirectTopicsQueryService directTopics) => _directTopics = directTopics;

    public Task<List<AvailableMentorDto>> Handle(GetAvailableMentorsQuery request, CancellationToken cancellationToken)
        => _directTopics.GetAvailableMentorsAsync(cancellationToken);
}
