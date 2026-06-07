using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Evaluation;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitFinalDecision;

public class SubmitFinalDecisionCommandHandler : ICommandHandler<SubmitFinalDecisionCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IBackgroundJobService _backgroundJobService;

    public SubmitFinalDecisionCommandHandler(
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IProjectRepository projectRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IBackgroundJobService backgroundJobService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _projectRepository = projectRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<Unit> Handle(SubmitFinalDecisionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;

        // 1. Verify current user is DepartmentHead
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        if (!currentUser.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var department = await _departmentRepository.GetByIdAsync(currentUser.DepartmentId.Value, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Department), currentUser.DepartmentId.Value);

        // 2. Load project
        var project = await _projectRepository.GetWithMentorsAsync(request.ProjectId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Project), request.ProjectId);

        // 3. Validate project belongs to CNBM's department
        var isInDepartment = await _departmentRepository.IsMajorInDepartmentAsync(
            project.MajorId, currentUser.DepartmentId.Value, cancellationToken);

        if (!isInDepartment)
            throw new BusinessRuleValidationException("This project does not belong to your department.");

        // 4. Verify 2 evaluators submitted with conflicting results
        var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(request.ProjectId, cancellationToken)).ToList();
        var submittedAssignments = assignments.Where(a => a.HasSubmittedEvaluation).ToList();

        if (submittedAssignments.Count < 2)
            throw new BusinessRuleValidationException("Not all evaluators have submitted their results yet.");

        var distinctResults = submittedAssignments.Select(a => a.IndividualResult).Distinct().ToList();
        if (distinctResults.Count < 2)
            throw new BusinessRuleValidationException("Evaluators have the same result. No final decision needed.");

        // 5. Apply the decision
        var finalResult = (EvaluationResult)request.Result;
        switch (finalResult)
        {
            case EvaluationResult.Approved:
                project.Approve();
                break;
            case EvaluationResult.NeedsModification:
                project.RequestModification();
                break;
            case EvaluationResult.Rejected:
                project.Reject();
                // Schedule cancellation after 5 minutes
                _backgroundJobService.Schedule<IProjectRepository>(
                    repo => repo.CancelRejectedProjectAsync(request.ProjectId, default),
                    TimeSpan.FromMinutes(5));
                break;
            default:
                throw new ArgumentException("Invalid result. Must be Approved, NeedsModification, or Rejected.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Publish domain event for notification handling
        await _publisher.Publish(
            new DepartmentHeadFinalDecisionEvent(request.ProjectId, finalResult, currentUserId),
            cancellationToken);

        return Unit.Value;
    }
}
