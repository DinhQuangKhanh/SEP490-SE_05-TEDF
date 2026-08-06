using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Domain.Enums.Project;
using ICurrentUserService = TEDF.Application.Common.Interfaces.ICurrentUserService;

namespace TEDF.Persistence.SqlServer.Interceptors
{
    /// <summary>
    /// Writes the project approval audit trail into SQL Server.
    /// </summary>
    /// <remarks>
    /// Runs on <c>SavingChanges</c> so the audit rows join the same transaction as the state
    /// change they describe — the trail can never drift from the projects it audits.
    /// It reads the pending domain events but does not clear them; <see cref="DomainEventInterceptor"/>
    /// still dispatches them after the save.
    /// </remarks>
    public class ProjectAuditLogInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public ProjectAuditLogInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            AddAuditLogs(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AddAuditLogs(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AddAuditLogs(DbContext? context)
        {
            if (context is null) return;

            // Entries() runs DetectChanges, so pending events and modified properties are both visible.
            var entries = context.ChangeTracker.Entries().ToList();

            var domainEvents = entries
                .Select(e => e.Entity)
                .OfType<IHasDomainEvents>()
                .SelectMany(ar => ar.DomainEvents)
                .ToList();

            if (domainEvents.Count == 0) return;

            var statusChanges = CollectStatusChanges(entries);
            var userId = _currentUserService.UserId;
            var userName = _currentUserService.FullName;

            var logs = new List<ProjectAuditLog>();

            foreach (var domainEvent in domainEvents)
            {
                var entry = Map(domainEvent);
                if (entry is null) continue;

                var (projectId, action, submissionNumber, metadata) = entry.Value;
                var (oldStatus, newStatus) = statusChanges.TryGetValue(projectId, out var transition)
                    ? transition
                    : (null, null);

                logs.Add(ProjectAuditLog.Create(
                    projectId,
                    action,
                    new ProjectAuditActor(userId, userName),
                    new ProjectStatusTransition(oldStatus, newStatus),
                    submissionNumber,
                    metadata is null ? null : JsonSerializer.Serialize(metadata)));
            }

            if (logs.Count > 0)
                context.Set<ProjectAuditLog>().AddRange(logs);
        }

        /// <summary>
        /// Reads the actual persisted status transition per project from the change tracker,
        /// which answers "who moved this project from NeedsModification to Approved".
        /// </summary>
        private static Dictionary<Guid, (ProjectStatus? Old, ProjectStatus? New)> CollectStatusChanges(
            IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries)
        {
            var changes = new Dictionary<Guid, (ProjectStatus?, ProjectStatus?)>();

            foreach (var entry in entries)
            {
                if (entry.Entity is not Domain.Aggregates.ProjectAggregate.Project project) continue;
                if (entry.State != EntityState.Modified) continue;

                var statusProperty = entry.Property(nameof(Domain.Aggregates.ProjectAggregate.Project.Status));
                if (!statusProperty.IsModified) continue;

                changes[project.Id] = (
                    (ProjectStatus?)statusProperty.OriginalValue,
                    (ProjectStatus?)statusProperty.CurrentValue);
            }

            return changes;
        }

        /// <summary>
        /// Maps an auditable domain event onto an audit row. Events that are not part of the
        /// approval workflow return null and are skipped.
        /// </summary>
        private static (Guid ProjectId, ProjectAuditAction Action, int? SubmissionNumber, object? Metadata)? Map(
            IDomainEvent domainEvent) => domainEvent switch
        {
            ProjectSubmittedEvent e =>
                (e.ProjectId, ProjectAuditAction.Submitted, e.SubmissionNumber, null),

            ProjectResubmittedEvent e =>
                (e.ProjectId, ProjectAuditAction.Resubmitted, e.SubmissionNumber, null),

            ProjectApprovedEvent e =>
                (e.ProjectId, ProjectAuditAction.Approved, null, null),

            ProjectModificationRequestedEvent e =>
                (e.ProjectId, ProjectAuditAction.NeedsModification, null, null),

            ProjectRejectedEvent e =>
                (e.ProjectId, ProjectAuditAction.Rejected, null, null),

            MentorAssignedEvent e =>
                (e.ProjectId, ProjectAuditAction.MentorAssigned, null, new { e.MentorId }),

            EvaluatorAssignedToProjectEvent e =>
                (e.ProjectId, ProjectAuditAction.EvaluatorAssigned, null,
                    new { e.EvaluatorId, e.PhaseId, e.EvaluatorOrder, e.AssignedBy }),

            DocumentUploadedEvent e =>
                (e.ProjectId, ProjectAuditAction.DocumentUploaded, null,
                    new { e.DocumentId, DocumentType = e.DocumentType.ToString() }),

            DocumentDeletedEvent e =>
                (e.ProjectId, ProjectAuditAction.DocumentDeleted, null,
                    new { e.DocumentId, e.DeletedBy }),

            _ => null
        };
    }
}
