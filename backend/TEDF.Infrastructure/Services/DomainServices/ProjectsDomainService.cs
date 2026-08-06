using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate.ValueObjects;
using TEDF.Domain.Aggregates.GroupAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices
{
    public class ProjectsDomainService : IProjectsDomainService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMajorReadRepository _majorRepository;
        private readonly IDateTimeService _dateTimeService;

        public ProjectsDomainService(
            IProjectRepository projectRepository,
            IGroupRepository groupRepository,
            ISemesterRepository semesterRepository,
            IMajorReadRepository majorRepository,
            IDateTimeService dateTimeService)
        {
            _projectRepository = projectRepository;
            _groupRepository = groupRepository;
            _semesterRepository = semesterRepository;
            _majorRepository = majorRepository;
            _dateTimeService = dateTimeService;
        }

        /// <inheritdoc/>
        public async Task<ProjectCode> GenerateProjectCodeAsync(int semesterId, int majorId, CancellationToken cancellationToken = default)
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Semester), semesterId);
            var major = await _majorRepository.GetByIdAsync(majorId, cancellationToken)
                ?? throw new EntityNotFoundException(nameof(Major), majorId);

            var prefix = ProjectCode.BuildPrefix(semester.Code.ShortValue, major.Code);
            var sequence = await _projectRepository.GetNextSequenceAsync(semesterId, prefix, cancellationToken);

            return ProjectCode.Generate(semester.Code.ShortValue, major.Code, sequence);
        }

        public async Task<(bool IsValid, string[] Errors)> ValidateForSubmissionAsync(Guid projectId, CancellationToken ct = default)
        {
            var errors = new List<string>();
            var project = await _projectRepository.GetWithMentorsAsync(projectId, ct);

            if (project is null)
                return (false, ["Đề tài không tồn tại."]);

            if (project.Status != ProjectStatus.Draft && project.Status != ProjectStatus.NeedsModification)
                errors.Add("Đề tài chỉ có thể nộp khi ở trạng thái Nháp hoặc Cần chỉnh sửa.");

            if (!project.Mentors.Any(m => m.IsActive))
                errors.Add("Đề tài phải có ít nhất một giảng viên hướng dẫn.");

            if (string.IsNullOrWhiteSpace(project.Description))
                errors.Add("Mô tả đề tài không được để trống.");

            if (string.IsNullOrWhiteSpace(project.Objectives))
                errors.Add("Mục tiêu đề tài không được để trống.");

            if (project.NameVi is null || string.IsNullOrWhiteSpace(project.NameVi.Value))
                errors.Add("Tên đề tài tiếng Việt không được để trống.");

            if (project.NameEn is null || string.IsNullOrWhiteSpace(project.NameEn.Value))
                errors.Add("Tên đề tài tiếng Anh không được để trống.");

            return (errors.Count == 0, errors.ToArray());
        }

        public Task<ProjectSnapshot> CreateSnapshotAsync(Project project, CancellationToken ct = default)
        {
            var snapshot = ProjectSnapshot.Capture(
                project.NameVi.Value,
                project.NameEn.Value,
                project.NameAbbr,
                project.Description,
                project.Objectives,
                project.Scope ?? string.Empty,
                project.Technologies?.Value ?? string.Empty,
                project.ExpectedResults ?? string.Empty,
                _dateTimeService.UtcNow
            );

            return Task.FromResult(snapshot);
        }

        public async Task<ProjectStatistics> GetStatisticsAsync(int semesterId, CancellationToken ct = default)
        {
            var statusCounts = await _projectRepository.GetStatusCountBySemesterAsync(semesterId, ct);

            var sourceCounts = await _projectRepository.GetSourceTypeCountBySemesterAsync(semesterId, ct);

            int Count(ProjectStatus s) => statusCounts.GetValueOrDefault(s);

            return new ProjectStatistics(
                TotalProjects: statusCounts.Values.Sum(),
                DraftProjects: Count(ProjectStatus.Draft),
                PendingEvaluationProjects: Count(ProjectStatus.PendingEvaluation),
                ApprovedProjects: Count(ProjectStatus.Approved),
                RejectedProjects: Count(ProjectStatus.Rejected),
                InProgressProjects: Count(ProjectStatus.InProgress),
                CompletedProjects: Count(ProjectStatus.Completed),
                CancelledProjects: Count(ProjectStatus.Cancelled),
                FromPoolCount: sourceCounts.GetValueOrDefault(ProjectSourceType.FromPool),
                DirectRegistrationCount: sourceCounts.GetValueOrDefault(ProjectSourceType.DirectRegistration)
            );
        }
    }
}
