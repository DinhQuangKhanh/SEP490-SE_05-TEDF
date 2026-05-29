using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Features.Dashboard.Queries.GetMentorDashboard;

public class GetMentorDashboardQueryHandler : IQueryHandler<GetMentorDashboardQuery, MentorDashboardDto>
{
    private readonly IMentorDashboardQueryService _queryService;
    private readonly ICurrentUserService _currentUser;

    public GetMentorDashboardQueryHandler(
        IMentorDashboardQueryService queryService,
        ICurrentUserService currentUser)
    {
        _queryService = queryService;
        _currentUser = currentUser;
    }

    public async Task<MentorDashboardDto> Handle(
        GetMentorDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _queryService.GetDashboardAsync(
            _currentUser.UserId.Value, cancellationToken);
    }
}
