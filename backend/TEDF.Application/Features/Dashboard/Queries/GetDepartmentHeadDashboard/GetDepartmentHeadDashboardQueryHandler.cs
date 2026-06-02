using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;

namespace TEDF.Application.Features.Dashboard.Queries.GetDepartmentHeadDashboard;

public class GetDepartmentHeadDashboardQueryHandler
    : IQueryHandler<GetDepartmentHeadDashboardQuery, DepartmentHeadDashboardDto>
{
    private readonly IDepartmentHeadQueryService _queryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public GetDepartmentHeadDashboardQueryHandler(
        IDepartmentHeadQueryService queryService,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _queryService = queryService;
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    public async Task<DepartmentHeadDashboardDto> Handle(
        GetDepartmentHeadDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("User is not assigned to any department.");

        return await _queryService.GetDashboardAsync(
            user.DepartmentId.Value, _currentUser.UserId.Value, cancellationToken);
    }
}
