using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Evaluations.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetEvaluatorHistory;

public class GetEvaluatorHistoryQueryHandler : IQueryHandler<GetEvaluatorHistoryQuery, EvaluatorHistoryDto>
{
    private readonly IEvaluatorQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetEvaluatorHistoryQueryHandler(
        IEvaluatorQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<EvaluatorHistoryDto> Handle(
        GetEvaluatorHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetHistoryAsync(
            _currentUser.UserId.Value,
            request.Page,
            request.PageSize,
            request.Search,
            request.Result,
            request.DateRange,
            cancellationToken);
    }
}
