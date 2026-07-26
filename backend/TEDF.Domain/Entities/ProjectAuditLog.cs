using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.Project;

namespace TEDF.Domain.Entities
{
    /// <summary>Who performed an audited action, with a name snapshot taken at write time.</summary>
    /// <param name="UserId">Null for actions triggered by a background job.</param>
    /// <param name="FullName">Snapshot so the trail survives a renamed or removed user.</param>
    public readonly record struct ProjectAuditActor(Guid? UserId, string? FullName);

    /// <summary>Project status before and after an audited action; both null when it did not change the status.</summary>
    public readonly record struct ProjectStatusTransition(ProjectStatus? Old, ProjectStatus? New);

    /// <summary>
    /// Append-only audit record for the project approval workflow (who approved, who changed
    /// the status, who assigned a reviewer, who deleted a document, how many revisions).
    /// </summary>
    /// <remarks>
    /// Stored in SQL Server so the trail is written inside the same transaction as the state
    /// change it describes: an audit row exists if and only if the change was committed.
    /// Rows are never updated or deleted — the entity exposes no mutators.
    /// </remarks>
    public class ProjectAuditLog : Entity<Guid>
    {
        public Guid ProjectId { get; private set; }

        public ProjectAuditAction Action { get; private set; }

        /// <summary>User who performed the action; null for actions triggered by a background job.</summary>
        public Guid? PerformedBy { get; private set; }

        /// <summary>Name snapshot taken at write time, so the trail survives a renamed or removed user.</summary>
        public string? PerformedByName { get; private set; }

        /// <summary>Project status before the change, when this action changed the status.</summary>
        public ProjectStatus? OldStatus { get; private set; }

        /// <summary>Project status after the change, when this action changed the status.</summary>
        public ProjectStatus? NewStatus { get; private set; }

        /// <summary>Submission attempt the action belongs to, for submit/resubmit actions.</summary>
        public int? SubmissionNumber { get; private set; }

        /// <summary>Action-specific details (document id, evaluator id, feedback...) as JSON.</summary>
        public string? MetadataJson { get; private set; }

        public DateTime Timestamp { get; private set; }

        private ProjectAuditLog() { }

        public static ProjectAuditLog Create(
            Guid projectId,
            ProjectAuditAction action,
            ProjectAuditActor actor,
            ProjectStatusTransition statusTransition = default,
            int? submissionNumber = null,
            string? metadataJson = null)
        {
            return new ProjectAuditLog
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Action = action,
                PerformedBy = actor.UserId,
                PerformedByName = actor.FullName,
                OldStatus = statusTransition.Old,
                NewStatus = statusTransition.New,
                SubmissionNumber = submissionNumber,
                MetadataJson = metadataJson,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
