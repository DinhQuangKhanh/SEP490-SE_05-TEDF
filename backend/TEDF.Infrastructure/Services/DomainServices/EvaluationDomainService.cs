using TEDF.Domain.Aggregates.EvaluationAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Evaluation;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices
{
    public class EvaluationDomainService : IEvaluationDomainService
    {
        private readonly IEvaluationSubmissionRepository _submissionRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IDateTimeService _dateTimeService;
        private const int MaxResubmissions = 3;
        private const int ModificationDeadlineDays = 14;

        public EvaluationDomainService(
            IEvaluationSubmissionRepository submissionRepository,
            IProjectRepository projectRepository,
            IDateTimeService dateTimeService)
        {
            _submissionRepository = submissionRepository;
            _projectRepository = projectRepository;
            _dateTimeService = dateTimeService;
        }

        public async Task<bool> CanResubmitAsync(Guid projectId, CancellationToken ct = default)
        {
            var remaining = await GetRemainingResubmissionsAsync(projectId, ct);
            var deadlinePassed = await IsModificationDeadlinePassedAsync(projectId, ct);
            return remaining > 0 && !deadlinePassed;
        }

        public async Task<int> GetRemainingResubmissionsAsync(Guid projectId, CancellationToken ct = default)
        {
            var submissionCount = await _submissionRepository.GetSubmissionCountByProjectIdAsync(projectId, ct);
            return Math.Max(0, MaxResubmissions - submissionCount + 1); // +1 because first submission is not a resubmission
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
            // Simple workload-based suggestion
            var submission = await _submissionRepository.GetByIdAsync(submissionId, ct);
            if (submission is null) return null;

            var project = await _projectRepository.GetByIdAsync(submission.ProjectId, ct);
            if (project is null) return null;

            // Get evaluators with least workload in the same department
            var evaluatorWorkloads = await _submissionRepository.GetActiveEvaluatorWorkloadCountsAsync(ct);

            // This is simplified - in real implementation, filter by department and expertise
            if (evaluatorWorkloads.Count == 0) return null;

            var leastLoaded = evaluatorWorkloads.MinBy(e => e.Value);
            return leastLoaded.Key;
        }
    }
}
