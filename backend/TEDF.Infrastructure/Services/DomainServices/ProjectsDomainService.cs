using TEDF.Domain.Aggregates.ProjectAggregate;
using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Aggregates.SemesterAggregate;
using TEDF.Domain.Common.Exceptions;
using TEDF.Domain.Entities;
using TEDF.Domain.Services;

namespace TEDF.Infrastructure.Services.DomainServices
{
    public sealed class ProjectsDomainService : IProjectsDomainService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMajorReadRepository _majorRepository;

        public ProjectsDomainService(
            IProjectRepository projectRepository,
            ISemesterRepository semesterRepository,
            IMajorReadRepository majorRepository)
        {
            _projectRepository = projectRepository;
            _semesterRepository = semesterRepository;
            _majorRepository = majorRepository;
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
    }
}
