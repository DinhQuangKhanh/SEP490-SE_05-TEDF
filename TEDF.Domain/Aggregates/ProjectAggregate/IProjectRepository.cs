using TEDF.Domain.Aggregates.ProjectAggregate.ValueObjects;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Enums.Document;
using TEDF.Domain.Enums.Project;
using TEDF.Domain.Enums.TopicPool;

namespace TEDF.Domain.Aggregates.ProjectAggregate
{
    public interface IProjectRepository : IRepository<Project, Guid>
    {
        /// <summary>
        /// Gets a project by its code.
        /// </summary>
        Task<Project?> GetByCodeAsync(ProjectCode code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a project with its mentors loaded.
        /// </summary>
        Task<Project?> GetWithMentorsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a project with its documents loaded.
        /// </summary>
        Task<Project?> GetWithDocumentsAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a project with all related entities loaded.
        /// </summary>
        Task<Project?> GetWithAllAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects by mentor identifier.
        /// </summary>
        Task<IEnumerable<Project>> GetByMentorIdAsync(Guid mentorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects by semester identifier.
        /// </summary>
        Task<IEnumerable<Project>> GetBySemesterIdAsync(int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects by status.
        /// </summary>
        Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects pending evaluation.
        /// </summary>
        Task<IEnumerable<Project>> GetPendingEvaluationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects by group identifier.
        /// </summary>
        Task<Project?> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects by major identifier.
        /// </summary>
        Task<IEnumerable<Project>> GetByMajorIdAsync(int majorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a project code already exists.
        /// </summary>
        Task<bool> ExistsCodeAsync(ProjectCode code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the next sequence number for generating project codes.
        /// </summary>
        Task<int> GetNextSequenceAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of projects by status for a semester.
        /// </summary>
        Task<Dictionary<ProjectStatus, int>> GetStatusCountBySemesterAsync(int semesterId, CancellationToken cancellationToken = default);

        Task<Dictionary<ProjectSourceType, int>> GetSourceTypeCountBySemesterAsync(int semesterId, CancellationToken cancellationToken = default);

        Task<int> CountActivePoolTopicsByMentorAsync(Guid topicPoolId, Guid mentorId, CancellationToken cancellationToken = default);

        Task<Dictionary<PoolTopicStatus, int>> GetPoolStatusCountsAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

        Task<List<Guid>> GetPoolProjectIdsAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

        Task<List<int>> GetMentorTopicCountsInPoolAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

        Task<List<Project>> GetExpirablePoolTopicsAsync(int currentSemesterId, CancellationToken cancellationToken = default);

        Task<List<Project>> GetPoolTopicsMissingExpirationAsync(CancellationToken cancellationToken = default);

        Task<List<Guid>> GetAvailableApprovedPoolTopicIdsAsync(Guid topicPoolId, CancellationToken cancellationToken = default);

        Task<List<Project>> GetExpiringPoolTopicsWithMentorsAsync(int currentSemesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a rejected project (sets status to Cancelled).
        /// Called by background job after 5-minute delay.
        /// </summary>
        Task CancelRejectedProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all projects pending evaluation for a given department (via Major.DepartmentId).
        /// </summary>
        Task<IEnumerable<Project>> GetPendingEvaluationByDepartmentAsync(int departmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a Document row directly (bypassing the aggregate's change tracker)
        /// and touches the Project's UpdatedAt. Used by background scan jobs to avoid
        /// DbUpdateConcurrencyException when multiple jobs hit the same aggregate.
        /// Returns <c>true</c> if inserted; <c>false</c> if a Document with the same
        /// file path already exists (idempotent).
        /// </summary>
        Task<bool> InsertDocumentAsync(
            Guid projectId, string fileName, string originalFileName,
            string fileType, long fileSize, string filePath,
            DocumentType documentType, Guid uploadedBy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the number of active projects (not Cancelled/Rejected) where the mentor
        /// is an active ProjectMentor in the given semester.
        /// Used to enforce MentorCannotExceedMaxGroupsPerSemesterRule.
        /// </summary>
        Task<int> CountMentorActiveProjectsInSemesterAsync(Guid mentorId, int semesterId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of projects with optional filters.
        /// Search matches against NameVi, NameEn, Code, and NameAbbr.
        /// </summary>
        Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(
            string? search, int? semesterId, ProjectStatus? status, int? majorId,
            int page, int pageSize, CancellationToken ct = default);
    }
}
