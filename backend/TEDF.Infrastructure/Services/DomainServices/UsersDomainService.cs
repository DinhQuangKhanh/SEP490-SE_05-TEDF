using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Users feature. See <see cref="IUsersDomainService"/>.
/// </summary>
public class UsersDomainService : IUsersDomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuthAccountService _authAccount;
    private readonly IUnitOfWork _unitOfWork;

    public UsersDomainService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IAuthAccountService authAccount,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _authAccount = authAccount;
        _unitOfWork = unitOfWork;
    }

    public async Task LockAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        // Prevent admin from locking themselves.
        if (actingUserId == userId)
            throw new BusinessRuleValidationException("Cannot lock your own account.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.Lock();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Disable the auth account before committing DB changes; if this fails the transaction is not persisted.
        await _authAccount.DisableAccountAsync(user.FirebaseUid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.Activate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _authAccount.EnableAccountAsync(user.FirebaseUid, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignDepartmentHeadAsync(int departmentId, Guid userId, Guid assignedBy, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), departmentId);

        var newHead = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        var newHeadRoles = newHead.GetActiveRoles().ToList();
        if (!newHeadRoles.Contains(DomainRoleNames.Mentor) && !newHeadRoles.Contains(DomainRoleNames.Evaluator))
            throw new BusinessRuleValidationException(
                "User must have Mentor or Evaluator role to be assigned as Department Head.");

        if (newHead.DepartmentId != departmentId)
            throw new BusinessRuleValidationException("User does not belong to this department.");

        // Remove DepartmentHead role from the previous head (if any).
        if (department.HeadOfDepartmentId.HasValue && department.HeadOfDepartmentId.Value != userId)
        {
            var oldHead = await _userRepository.GetByIdAsync(department.HeadOfDepartmentId.Value, cancellationToken);
            if (oldHead != null)
            {
                oldHead.RemoveRole(DomainRoleIds.DepartmentHead);
                await _userRepository.UpdateAsync(oldHead, cancellationToken);
            }
        }

        newHead.AssignRole(DomainRoleIds.DepartmentHead, DomainRoleNames.DepartmentHead, assignedBy);
        await _userRepository.UpdateAsync(newHead, cancellationToken);

        department.SetHeadOfDepartment(userId);
        _departmentRepository.Update(department);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMyProfileAsync(Guid userId, string? phoneNumber, DateOnly? birthDate, string? privacySettings, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        user.UpdateProfile(
            phoneNumber: phoneNumber,
            birthDate: birthDate,
            privacySettings: privacySettings);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
