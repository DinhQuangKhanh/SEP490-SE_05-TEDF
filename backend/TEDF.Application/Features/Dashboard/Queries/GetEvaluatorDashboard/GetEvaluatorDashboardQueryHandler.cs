using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetEvaluatorDashboard;

public class GetEvaluatorDashboardQueryHandler : IQueryHandler<GetEvaluatorDashboardQuery, EvaluatorDashboardDto>
{
    private readonly IDashboardQueryService _dashboard;
    private readonly ICurrentUserService _currentUser;

    public GetEvaluatorDashboardQueryHandler(IDashboardQueryService dashboard, ICurrentUserService currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    public Task<EvaluatorDashboardDto> Handle(GetEvaluatorDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _dashboard.GetEvaluatorDashboardAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
