using MediatR;
using TEDF.Application.Common.Abstractions;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Enums.Evaluation;
using IUnitOfWork = TEDF.Domain.Common.Interfaces.IUnitOfWork;

namespace TEDF.Application.Features.Evaluations.Commands.SubmitEvaluation;

public class SubmitEvaluationCommandHandler : ICommandHandler<SubmitEvaluationCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IPublisher _publisher;
    private readonly IBackgroundJobService _backgroundJobService;

    public SubmitEvaluationCommandHandler(
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IProjectRepository projectRepository,
        IPublisher publisher,
        IBackgroundJobService backgroundJobService)
    {
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _publisher = publisher;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<Unit> Handle(SubmitEvaluationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var evaluatorId = _currentUser.UserId.Value;

        var assignment = await _assignmentRepository.GetActiveByProjectAndEvaluatorAsync(
            request.ProjectId, evaluatorId, cancellationToken)

             ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");
        var result = (EvaluationResult)request.Result;
        assignment.SubmitEvaluation(result, request.Feedback);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Check if both evaluators have submitted — resolve automatically if results match
        var allAssignments = (await _assignmentRepository.GetActiveByProjectIdAsync(
            request.ProjectId, cancellationToken)).ToList();
        var submittedAssignments = allAssignments.Where(a => a.HasSubmittedEvaluation).ToList();

        if (submittedAssignments.Count >= 2)
        {
            var results = submittedAssignments.Select(a => a.IndividualResult!.Value).Distinct().ToList();

            if (results.Count == 1)
            {
                // Both evaluators agree — auto-resolve
                var project = await _projectRepository.GetWithMentorsAsync(request.ProjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Project not found.");

                var agreedResult = results[0];
                switch (agreedResult)
                {
                    case EvaluationResult.Approved:
                        project.Approve();
                        break;
                    case EvaluationResult.NeedsModification:
                        project.RequestModification();
                        break;
                    case EvaluationResult.Rejected:
                        project.Reject();
                        _backgroundJobService.Schedule<IProjectRepository>(
                            repo => repo.CancelRejectedProjectAsync(request.ProjectId, default),
                            TimeSpan.FromMinutes(5));
                        break;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            // If results differ — no auto-resolve, CNBM must decide
        }

        // Publish event for notification handling (always, regardless of resolution)
        await _publisher.Publish(
            new EvaluatorSubmittedResultEvent(assignment.Id, request.ProjectId, evaluatorId, result),
            cancellationToken);

        return Unit.Value;
    }
}
