using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.Application.Features.DirectTopics.Queries.GetAvailableMentors;

public class GetAvailableMentorsQueryHandler : IQueryHandler<GetAvailableMentorsQuery, AvailableMentorsResponse>
{
    private readonly IDirectTopicsQueryService _directTopics;

    public GetAvailableMentorsQueryHandler(IDirectTopicsQueryService directTopics) => _directTopics = directTopics;

    public Task<AvailableMentorsResponse> Handle(GetAvailableMentorsQuery request, CancellationToken cancellationToken)
        => _directTopics.GetAvailableMentorsAsync(request.GroupId, cancellationToken);
}
