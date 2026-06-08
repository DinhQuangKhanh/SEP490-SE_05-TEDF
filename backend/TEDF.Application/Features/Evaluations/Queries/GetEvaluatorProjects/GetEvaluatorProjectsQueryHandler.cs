using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetEvaluatorProjects;

public class GetEvaluatorProjectsQueryHandler : IQueryHandler<GetEvaluatorProjectsQuery, EvaluatorProjectsDto>
{
    private readonly IEvaluationsQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetEvaluatorProjectsQueryHandler(
        IEvaluationsQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<EvaluatorProjectsDto> Handle(
        GetEvaluatorProjectsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetProjectsAsync(
            _currentUser.UserId.Value,
            request.Page,
            request.PageSize,
            request.Search,
            request.SemesterId,
            request.MajorId,
            request.Result,
            cancellationToken);
    }
}
