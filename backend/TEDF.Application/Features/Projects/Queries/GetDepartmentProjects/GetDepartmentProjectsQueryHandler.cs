using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Projects.DTOs;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;

namespace TEDF.Application.Features.Projects.Queries.GetDepartmentProjects;

public class GetDepartmentProjectsQueryHandler : IQueryHandler<GetDepartmentProjectsQuery, DepartmentProjectsResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentHeadQueryService _queryService;

    public GetDepartmentProjectsQueryHandler(
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IDepartmentHeadQueryService queryService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _queryService = queryService;
    }

    public async Task<DepartmentProjectsResponse> Handle(
        GetDepartmentProjectsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUser = await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        if (!currentUser.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var department = await _departmentRepository.GetByIdAsync(currentUser.DepartmentId.Value, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), currentUser.DepartmentId.Value);

        return await _queryService.GetDepartmentProjectsAsync(
            currentUser.DepartmentId.Value, cancellationToken);
    }
}
