using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Constants;
using TEDF.Domain.Enums.User;

namespace TEDF.Infrastructure.Authorization;

/// <summary>
/// Implements <see cref="IAccessControlService"/>. Returns a fresh decision (the gate middleware
/// caches it briefly; the /api/auth/session endpoint calls it directly for an up-to-date answer).
/// </summary>
public class AccessControlService : IAccessControlService
{
    private readonly IUserRepository _userRepository;
    private readonly ISemesterRepository _semesterRepository;

    public AccessControlService(IUserRepository userRepository, ISemesterRepository semesterRepository)
    {
        _userRepository = userRepository;
        _semesterRepository = semesterRepository;
    }

    public async Task<AccessDecision> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return new AccessDecision(false, "inactive", "Tài khoản không tồn tại.");

        if (user.Status == UserStatus.Locked)
            return new AccessDecision(false, "locked", "Tài khoản của bạn đã bị khóa.");
        if (user.Status == UserStatus.Inactive)
            return new AccessDecision(false, "inactive", "Tài khoản của bạn đã bị vô hiệu hóa.");

        var roles = user.GetActiveRoles().ToHashSet();
        var isStaff = roles.Contains(DomainRoleNames.Admin)
            || roles.Contains(DomainRoleNames.DepartmentHead)
            || roles.Contains(DomainRoleNames.Mentor)
            || roles.Contains(DomainRoleNames.Evaluator);

        // Student-only accounts must be on the active/upcoming eligible list to use the system.
        if (roles.Contains(DomainRoleNames.Student) && !isStaff)
        {
            var eligible = await _semesterRepository.IsStudentEligibleNowAsync(userId, cancellationToken);
            if (!eligible)
                return new AccessDecision(false, "student_not_eligible",
                    "Bạn không thuộc danh sách sinh viên đủ điều kiện làm đồ án trong học kỳ hiện tại hoặc sắp tới.");
        }

        return new AccessDecision(true, null, null);
    }
}
