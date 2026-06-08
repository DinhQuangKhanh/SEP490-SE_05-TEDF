using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetMentorDashboard;

public class GetMentorDashboardQueryHandler : IQueryHandler<GetMentorDashboardQuery, MentorDashboardDto>
{
    private readonly IDashboardQueryService _dashboard;
    private readonly ICurrentUserService _currentUser;

    public GetMentorDashboardQueryHandler(IDashboardQueryService dashboard, ICurrentUserService currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    public Task<MentorDashboardDto> Handle(GetMentorDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _dashboard.GetMentorDashboardAsync(_currentUser.UserId.Value, cancellationToken);
    }
}
