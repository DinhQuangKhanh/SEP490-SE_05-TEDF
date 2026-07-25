using MediatR;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Aggregates.ProjectAggregate.Events;
using TEDF.Domain.Aggregates.EvaluationAggregate.Events;

namespace TEDF.Application.Features.Projects.EventHandlers;

public class ProjectAuditLogEventHandler :
    INotificationHandler<ProjectApprovedEvent>,
    INotificationHandler<ProjectModificationRequestedEvent>,
    INotificationHandler<ProjectRejectedEvent>,
    INotificationHandler<ProjectSubmittedEvent>,
    INotificationHandler<ProjectResubmittedEvent>,
    INotificationHandler<ProjectMentorApprovedEvent>,
    INotificationHandler<ProjectMentorRequestedModificationEvent>,
    INotificationHandler<ProjectSubmittedToMentorEvent>,
    INotificationHandler<MentorAssignedEvent>,
    INotificationHandler<DocumentUploadedEvent>,
    INotificationHandler<DocumentDeletedEvent>,
    INotificationHandler<EvaluatorAssignedToProjectEvent>
{
    private readonly ISystemAuditLogWriteService _auditLogWriteService;
    private readonly ICurrentUserService _currentUserService;

    public ProjectAuditLogEventHandler(
        ISystemAuditLogWriteService auditLogWriteService,
        ICurrentUserService currentUserService)
    {
        _auditLogWriteService = auditLogWriteService;
        _currentUserService = currentUserService;
    }

    private async Task LogAsync(Guid projectId, string action, object? metadata, CancellationToken ct)
    {
        await _auditLogWriteService.LogAsync("Project", projectId, action, _currentUserService.UserId, metadata, ct);
    }

    public Task Handle(ProjectApprovedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "Approved", null, cancellationToken);

    public Task Handle(ProjectModificationRequestedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "NeedsModification", null, cancellationToken);

    public Task Handle(ProjectRejectedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "Rejected", null, cancellationToken);

    public Task Handle(ProjectSubmittedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "Submitted", new { notification.SubmissionNumber }, cancellationToken);

    public Task Handle(ProjectResubmittedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "Resubmitted", new { notification.SubmissionNumber }, cancellationToken);

    public Task Handle(ProjectMentorApprovedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "MentorApproved", new { notification.MentorId }, cancellationToken);

    public Task Handle(ProjectMentorRequestedModificationEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "MentorNeedsModification", new { notification.Feedback }, cancellationToken);

    public Task Handle(ProjectSubmittedToMentorEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "SubmittedToMentor", null, cancellationToken);

    public Task Handle(MentorAssignedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "MentorAssigned", new { notification.MentorId }, cancellationToken);

    public Task Handle(DocumentUploadedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "DocumentUploaded", new { notification.DocumentId, DocumentType = notification.DocumentType.ToString() }, cancellationToken);

    public Task Handle(DocumentDeletedEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "DocumentDeleted", new { notification.DocumentId, notification.DeletedBy }, cancellationToken);

    public Task Handle(EvaluatorAssignedToProjectEvent notification, CancellationToken cancellationToken)
        => LogAsync(notification.ProjectId, "EvaluatorAssigned", new { notification.EvaluatorId, notification.PhaseId }, cancellationToken);
}
