using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Constants;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Departments.Commands.AssignDepartmentHead;

/// <summary>
/// Handles the AssignDepartmentHeadCommand.
/// 1. Validates that the target user is a lecturer (Mentor/Evaluator) belonging to the department.
/// 2. Removes the DepartmentHead role from the previous head (if any).
/// 3. Assigns the DepartmentHead role to the new user.
/// 4. Updates Department.HeadOfDepartmentId.
/// Firebase custom claims are synced automatically via domain event handlers.
/// </summary>
public class AssignDepartmentHeadCommandHandler : ICommandHandler<AssignDepartmentHeadCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AssignDepartmentHeadCommandHandler(
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(AssignDepartmentHeadCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        // 1. Load and validate department
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), request.DepartmentId);

        // 2. Load and validate the new head user
        var newHead = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.UserId);

        var newHeadRoles = newHead.GetActiveRoles().ToList();
        if (!newHeadRoles.Contains(DomainRoleNames.Mentor) && !newHeadRoles.Contains(DomainRoleNames.Evaluator))
            throw new BusinessRuleValidationException(
                "User must have Mentor or Evaluator role to be assigned as Department Head.");

        if (newHead.DepartmentId != request.DepartmentId)
            throw new BusinessRuleValidationException(
                "User does not belong to this department.");

        // 3. Remove DepartmentHead role from the previous head (if any)
        if (department.HeadOfDepartmentId.HasValue && department.HeadOfDepartmentId.Value != request.UserId)
        {
            var oldHead = await _userRepository.GetByIdAsync(department.HeadOfDepartmentId.Value, cancellationToken);
            if (oldHead != null)
            {
                oldHead.RemoveRole(DomainRoleNames.DepartmentHead);
                await _userRepository.UpdateAsync(oldHead, cancellationToken);
            }
        }

        // 4. Assign DepartmentHead role to the new user
        newHead.AssignRole(DomainRoleNames.DepartmentHead, _currentUser.UserId);
        await _userRepository.UpdateAsync(newHead, cancellationToken);

        // 5. Update department's HeadOfDepartmentId
        department.SetHeadOfDepartment(request.UserId);
        _departmentRepository.Update(department);

        // 6. Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
