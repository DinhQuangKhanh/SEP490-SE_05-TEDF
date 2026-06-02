using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Entities;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Constants;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;


namespace TEDF.Application.Features.Departments.Commands.AssignEvaluator;

/// <summary>
/// Handles the AssignEvaluatorCommand.
/// 1. Validates the current user is the DepartmentHead (CNBM) of their department.
/// 2. Validates the project belongs to the CNBM's department (via Project.MajorId → Major.DepartmentId).
/// 3. Validates the evaluator has the Evaluator role.
/// 4. Delegates to ProjectEvaluatorAssignment.Create() which enforces:
///    - Evaluator ≠ Mentor of the project
///    - Max 3 evaluators per project
/// </summary>
public class AssignEvaluatorCommandHandler : ICommandHandler<AssignEvaluatorCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public AssignEvaluatorCommandHandler(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IProjectRepository projectRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _projectRepository = projectRepository;
        _assignmentRepository = assignmentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(AssignEvaluatorCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;

        // 1. Load the current user and verify they are a DepartmentHead
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        if (!currentUser.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var departmentId = currentUser.DepartmentId.Value;

        // 2. Verify the current user is indeed the DepartmentHead of their department
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), departmentId);

        // 3. Load the project with mentors
        var project = await _projectRepository.GetWithMentorsAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // 4. Validate project belongs to the CNBM's department (via Major)
        var isInDepartment = await _departmentRepository.IsMajorInDepartmentAsync(
            project.MajorId, departmentId, cancellationToken);

        if (!isInDepartment)
            throw new BusinessRuleValidationException(
                "This project does not belong to your department. You can only assign evaluators to projects within your department.");

        // 5. Validate the evaluator exists and has the Evaluator role
        var evaluator = await _userRepository.GetByIdAsync(request.EvaluatorId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(User), request.EvaluatorId);

        if (!evaluator.GetActiveRoles().Contains(DomainRoleNames.Evaluator))
            throw new BusinessRuleValidationException("The specified user does not have the Evaluator role.");

        // 6. Get all mentor IDs for the project (for conflict check)
        var allProjectMentorIds = project.Mentors
            .Select(m => m.MentorId)
            .ToList()
            .AsReadOnly();

        // 7. Get current active evaluator count for the project
        var currentActiveEvaluatorCount = await _assignmentRepository
            .GetActiveCountByProjectIdAsync(request.ProjectId, cancellationToken);

        // 8. Create the assignment — business rules are enforced inside Create()
        var assignment = ProjectEvaluatorAssignment.Create(
            projectId: request.ProjectId,
            evaluatorId: request.EvaluatorId,
            order: request.EvaluatorOrder,
            assignedBy: currentUserId,
            allProjectMentorIds: allProjectMentorIds,
            currentActiveEvaluatorCount: currentActiveEvaluatorCount);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);

        // 9. Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 10. Publish domain event for notification handling
        await _publisher.Publish(new EvaluatorAssignedToProjectEvent(
            assignment.Id, request.ProjectId, request.EvaluatorId, request.EvaluatorOrder, currentUserId), cancellationToken);

        return Unit.Value;
    }
}
