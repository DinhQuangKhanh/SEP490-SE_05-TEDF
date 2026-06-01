using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.DTOs;

namespace TEDF.Application.Features.TopicPools.Queries.GetTopicPoolsByDepartment;

public class GetTopicPoolsByDepartmentQueryHandler
    : IQueryHandler<GetTopicPoolsByDepartmentQuery, List<DepartmentWithPoolsDto>>
{
    private readonly ITopicPoolQueryService _queryService;

    public GetTopicPoolsByDepartmentQueryHandler(ITopicPoolQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<List<DepartmentWithPoolsDto>> Handle(
        GetTopicPoolsByDepartmentQuery request,
        CancellationToken cancellationToken)
    {
        return await _queryService.GetPoolsByDepartmentAsync(cancellationToken);
    }
}
