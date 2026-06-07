using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Features.Evaluations.Queries.GetDepartmentEvaluators;

public class GetDepartmentEvaluatorsQueryHandler : IQueryHandler<GetDepartmentEvaluatorsQuery, List<DepartmentEvaluatorDto>>
{
    private readonly IEvaluationsQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetDepartmentEvaluatorsQueryHandler(IEvaluationsQueryService queryService, ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public Task<List<DepartmentEvaluatorDto>> Handle(GetDepartmentEvaluatorsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _queryService.GetDepartmentEvaluatorsAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
