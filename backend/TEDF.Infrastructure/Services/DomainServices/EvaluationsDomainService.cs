using MediatR;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.EvaluationAggregate.Entities;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.UserAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Constants;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Services;
using IDateTimeService = TEDF.Application.Common.Interfaces.IDateTimeService;

namespace TEDF.Infrastructure.Services.DomainServices;

/// <summary>
/// Write-side service for the Evaluations feature. See <see cref="IEvaluationsDomainService"/>.
/// </summary>
public class EvaluationsDomainService : IEvaluationsDomainService
{
    private const int MaxResubmissions = 3;
    private const int ModificationDeadlineDays = 14;

    private readonly IEvaluationSubmissionRepository _submissionRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IProjectEvaluatorAssignmentRepository _assignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IBackgroundJobService _backgroundJobService;

    public EvaluationsDomainService(
        IEvaluationSubmissionRepository submissionRepository,
        IProjectRepository projectRepository,
        IDateTimeService dateTimeService,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IProjectEvaluatorAssignmentRepository assignmentRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IBackgroundJobService backgroundJobService)
    {
        _submissionRepository = submissionRepository;
        _projectRepository = projectRepository;
        _dateTimeService = dateTimeService;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _assignmentRepository = assignmentRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _backgroundJobService = backgroundJobService;
    }

    // ── Helper queries / policies ──
    public async Task<bool> CanResubmitAsync(Guid projectId, CancellationToken ct = default)
    {
        var remaining = await GetRemainingResubmissionsAsync(projectId, ct);
        var deadlinePassed = await IsModificationDeadlinePassedAsync(projectId, ct);
        return remaining > 0 && !deadlinePassed;
    }

    public async Task<int> GetRemainingResubmissionsAsync(Guid projectId, CancellationToken ct = default)
    {
        var submissionCount = await _submissionRepository.GetSubmissionCountByProjectIdAsync(projectId, ct);
        return Math.Max(0, MaxResubmissions - submissionCount + 1);
    }

    public async Task<bool> IsModificationDeadlinePassedAsync(Guid projectId, CancellationToken ct = default)
    {
        var latestSubmission = await _submissionRepository.GetLatestByProjectIdAsync(projectId, ct);
        if (latestSubmission is null) return false;
        if (latestSubmission.Result != EvaluationResult.NeedsModification) return false;

        var deadline = latestSubmission.EvaluatedAt?.AddDays(ModificationDeadlineDays);
        return deadline.HasValue && _dateTimeService.UtcNow > deadline.Value;
    }

    public async Task<EvaluationStatistics> GetStatisticsAsync(int semesterId, CancellationToken ct = default)
    {
        var submissions = await _submissionRepository.GetBySemesterWithSnapshotAsync(semesterId, ct);

        var evaluatorWorkload = submissions
            .Where(s => s.AssignedEvaluatorId.HasValue)
            .GroupBy(s => s.AssignedEvaluatorId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var completedSubmissions = submissions.Where(s => s.Status == SubmissionStatus.Completed).ToList();
        var avgDays = completedSubmissions.Any()
            ? completedSubmissions.Average(s => (s.EvaluatedAt!.Value - s.SubmittedAt).TotalDays)
            : 0;

        return new EvaluationStatistics(
            TotalSubmissions: submissions.Count,
            PendingSubmissions: submissions.Count(s => s.Status == SubmissionStatus.Pending),
            InReviewSubmissions: submissions.Count(s => s.Status == SubmissionStatus.InReview),
            CompletedSubmissions: completedSubmissions.Count,
            ApprovedCount: submissions.Count(s => s.Result == EvaluationResult.Approved),
            NeedsModificationCount: submissions.Count(s => s.Result == EvaluationResult.NeedsModification),
            RejectedCount: submissions.Count(s => s.Result == EvaluationResult.Rejected),
            AverageEvaluationDays: avgDays,
            EvaluatorWorkload: evaluatorWorkload
        );
    }

    public async Task<Guid?> SuggestEvaluatorAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(submissionId, ct);
        if (submission is null) return null;

        var project = await _projectRepository.GetByIdAsync(submission.ProjectId, ct);
        if (project is null) return null;

        var evaluatorWorkloads = await _submissionRepository.GetActiveEvaluatorWorkloadCountsAsync(ct);
        if (evaluatorWorkloads.Count == 0) return null;

        var leastLoaded = evaluatorWorkloads.MinBy(e => e.Value);
        return leastLoaded.Key;
    }

    // ── Write operations ──
    public async Task AssignEvaluatorAsync(Guid currentUserId, Guid projectId, int phaseId, Guid evaluatorId, int evaluatorOrder, CancellationToken ct = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, ct)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        if (!currentUser.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        var departmentId = currentUser.DepartmentId.Value;

        _ = await _departmentRepository.GetByIdAsync(departmentId, ct)
            ?? throw new EntityNotFoundException(nameof(Department), departmentId);

        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        var isInDepartment = await _departmentRepository.IsMajorInDepartmentAsync(project.MajorId, departmentId, ct);
        if (!isInDepartment)
            throw new BusinessRuleValidationException(
                "This project does not belong to your department. You can only assign evaluators to projects within your department.");

        var evaluator = await _userRepository.GetByIdAsync(evaluatorId, ct)
            ?? throw new EntityNotFoundException(nameof(User), evaluatorId);

        if (!evaluator.GetActiveRoles().Contains(DomainRoleNames.Evaluator))
            throw new BusinessRuleValidationException("The specified user does not have the Evaluator role.");

        var allProjectMentorIds = project.Mentors.Select(m => m.MentorId).ToList().AsReadOnly();

        var currentActiveEvaluatorCount = await _assignmentRepository.GetActiveCountByProjectIdAsync(projectId, ct);

        var assignment = ProjectEvaluatorAssignment.Create(
            projectId: projectId,
            phaseId: phaseId,
            evaluatorId: evaluatorId,
            order: evaluatorOrder,
            assignedBy: currentUserId,
            allProjectMentorIds: allProjectMentorIds,
            currentActiveEvaluatorCount: currentActiveEvaluatorCount);

        await _assignmentRepository.AddAsync(assignment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.Publish(new EvaluatorAssignedToProjectEvent(
            assignment.Id, projectId, phaseId, evaluatorId, evaluatorOrder, currentUserId), ct);
    }

    public async Task SubmitEvaluationAsync(Guid evaluatorId, Guid projectId, int result, string? feedback, CancellationToken ct = default)
    {
        var assignment = await _assignmentRepository.GetActiveByProjectAndEvaluatorAsync(projectId, evaluatorId, ct)
            ?? throw new UnauthorizedAccessException("Bạn không được gán để thẩm định đề tài này.");

        var evalResult = (EvaluationResult)result;
        assignment.SubmitEvaluation(evalResult, feedback);

        await _unitOfWork.SaveChangesAsync(ct);

        // Auto-resolve when both evaluators have submitted matching results.
        var allAssignments = (await _assignmentRepository.GetActiveByProjectIdAsync(projectId, ct)).ToList();
        var submittedAssignments = allAssignments.Where(a => a.HasSubmittedEvaluation).ToList();

        if (submittedAssignments.Count >= 2)
        {
            var results = submittedAssignments.Select(a => a.IndividualResult!.Value).Distinct().ToList();
            if (results.Count == 1)
            {
                var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
                    ?? throw new InvalidOperationException("Project not found.");

                switch (results[0])
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
                            repo => repo.CancelRejectedProjectAsync(projectId, default),
                            TimeSpan.FromMinutes(5));
                        break;
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        await _publisher.Publish(
            new EvaluatorSubmittedResultEvent(assignment.Id, projectId, evaluatorId, evalResult), ct);
    }

    public async Task SubmitFinalDecisionAsync(Guid currentUserId, Guid projectId, int result, string? notes, CancellationToken ct = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, ct)
            ?? throw new UnauthorizedAccessException("Current user not found.");

        if (!currentUser.DepartmentId.HasValue)
            throw new BusinessRuleValidationException("Current user is not assigned to any department.");

        _ = await _departmentRepository.GetByIdAsync(currentUser.DepartmentId.Value, ct)
            ?? throw new EntityNotFoundException(nameof(Department), currentUser.DepartmentId.Value);

        var project = await _projectRepository.GetWithMentorsAsync(projectId, ct)
            ?? throw new EntityNotFoundException(nameof(Project), projectId);

        var isInDepartment = await _departmentRepository.IsMajorInDepartmentAsync(
            project.MajorId, currentUser.DepartmentId.Value, ct);
        if (!isInDepartment)
            throw new BusinessRuleValidationException("This project does not belong to your department.");

        var assignments = (await _assignmentRepository.GetActiveByProjectIdAsync(projectId, ct)).ToList();
        var submittedAssignments = assignments.Where(a => a.HasSubmittedEvaluation).ToList();

        if (submittedAssignments.Count < 2)
            throw new BusinessRuleValidationException("Not all evaluators have submitted their results yet.");

        var distinctResults = submittedAssignments.Select(a => a.IndividualResult).Distinct().ToList();
        if (distinctResults.Count < 2)
            throw new BusinessRuleValidationException("Evaluators have the same result. No final decision needed.");

        var finalResult = (EvaluationResult)result;
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
                _backgroundJobService.Schedule<IProjectRepository>(
                    repo => repo.CancelRejectedProjectAsync(projectId, default),
                    TimeSpan.FromMinutes(5));
                break;
            default:
                throw new ArgumentException("Invalid result. Must be Approved, NeedsModification, or Rejected.");
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _publisher.Publish(new DepartmentHeadFinalDecisionEvent(projectId, finalResult, currentUserId), ct);
    }
}
