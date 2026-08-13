using Microsoft.EntityFrameworkCore;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Users.DTOs;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Entities;

namespace TEDF.Persistence.SqlServer.QueryServices;

/// <summary>
/// Read-side service for the Users feature. See <see cref="IUsersQueryService"/>.
/// </summary>
public class UsersQueryService : IUsersQueryService
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly AppDbContext _context;

    public UsersQueryService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        AppDbContext context)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _context = context;
    }

    public async Task<GetUsersQueryResult> GetUsersAsync(
        string? role,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var (users, totalCount) = await _userRepository.GetPagedAsync(role, search, page, pageSize, cancellationToken);

        // Load all departments to build a name lookup (small table, safe to load all).
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        var deptMap = departments.ToDictionary(d => d.Id, d => d.Name);

        var items = users.Select(u => new UserListItemDto(
            u.Id,
            u.FullName,
            u.Email.Value,
            u.AvatarUrl,
            u.Student?.StudentCode,
            u.Lecturer?.EmployeeCode,
            u.Lecturer?.AcademicTitle,
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

    public async Task<MyProfileDto> GetMyProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new TEDF.Domain.Common.Exceptions.EntityNotFoundException(nameof(User), userId);

        string? deptName = null;
        if (user.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(user.DepartmentId.Value, cancellationToken);
            deptName = dept?.Name;
        }

        // A student never types their own major: it is snapshotted onto the semester roster when
        // the admin imports it, so the profile reads it back from the most recent roster entry.
        Major? major = null;
        if (user.Student is not null)
        {
            var majorId = await _context.EligibleStudents.AsNoTracking()
                .Where(es => es.StudentId == user.Id && es.MajorId != null)
                .Join(_context.Semesters.AsNoTracking(),
                    es => es.SemesterId, sem => sem.Id,
                    (es, sem) => new { es.MajorId, sem.StartDate })
                .OrderByDescending(x => x.StartDate)
                .Select(x => x.MajorId)
                .FirstOrDefaultAsync(cancellationToken);

            if (majorId.HasValue)
            {
                major = await _context.Majors.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == majorId.Value, cancellationToken);
            }
        }

        string? programCode = null;
        string? programName = null;
        var programId = user.Student?.ProgramId;
        if (programId.HasValue)
        {
            var program = await _context.Programs.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == programId.Value, cancellationToken);
            programCode = program?.Code;
            programName = program?.Name;
        }

        return new MyProfileDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email.Value,
            AvatarUrl: user.AvatarUrl,
            StudentCode: user.Student?.StudentCode,
            EmployeeCode: user.Lecturer?.EmployeeCode,
            PhoneNumber: user.PhoneNumber,
            BirthDate: user.BirthDate,
            PrivacySettings: user.PrivacySettings,
            AcademicTitle: user.Lecturer?.AcademicTitle,
            DepartmentId: user.DepartmentId,
            DepartmentName: deptName,
            MajorId: major?.Id,
            MajorCode: major?.Code,
            MajorName: major?.Name,
            ProgramId: programId,
            ProgramCode: programCode,
            ProgramName: programName,
            ComboId: user.Student?.ComboId,
            ComboName: user.Student?.Combo?.Name,
            Status: user.Status.ToString(),
            Roles: user.GetActiveRoles().ToList(),
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt
        );
    }
}
