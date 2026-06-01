using TEDF.Application.Common.Abstractions;
using TEDF.Application.Features.Users.DTOs;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;

namespace TEDF.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, GetUsersQueryResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public GetUsersQueryHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<GetUsersQueryResult> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (users, totalCount) = await _userRepository.GetPagedAsync(
            request.Role, request.Search, page, pageSize, cancellationToken);

        // Load all departments to build name lookup (small table, safe to load all)
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        var deptMap = departments.ToDictionary(d => d.Id, d => d.Name);

        var items = users.Select(u => new UserListItemDto(
            u.Id,
            u.FullName,
            u.Email.Value,
            u.AvatarUrl,
            u.StudentCode,
            u.EmployeeCode,
            u.AcademicTitle,
            u.DepartmentId,
            u.DepartmentId.HasValue && deptMap.TryGetValue(u.DepartmentId.Value, out var deptName)
                ? deptName
                : null,
            u.Status.ToString(),
            u.GetActiveRoles().ToList(),
            u.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new GetUsersQueryResult(items, totalCount, page, pageSize, totalPages);
    }
}
