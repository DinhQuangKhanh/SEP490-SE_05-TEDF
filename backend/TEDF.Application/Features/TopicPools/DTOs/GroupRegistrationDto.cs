namespace TEDF.Application.Features.TopicPools.DTOs;

/// <summary>
/// A topic-pool registration made by a student group, with the resolved topic/mentor info
/// and current status. Used by the student to track a pending/rejected registration.
/// </summary>
public class GroupRegistrationDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectCode { get; init; }
    public string? MentorName { get; init; }

    /// <summary>Registration status: Pending | Confirmed | Rejected | Cancelled.</summary>
    public string Status { get; init; } = string.Empty;

    public DateTime RegisteredAt { get; init; }
    public string? Note { get; init; }
    public string? RejectReason { get; init; }
}
