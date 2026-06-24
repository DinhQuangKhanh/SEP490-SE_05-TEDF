namespace TEDF.Application.Features.TopicPools.DTOs;

/// <summary>
/// A pending pool-topic registration request shown to the mentor (the "Yêu cầu đăng ký" tab),
/// with the resolved topic, group and requester info.
/// </summary>
public class MentorRegistrationRequestDto
{
    public Guid RegistrationId { get; init; }
    public Guid ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string? ProjectCode { get; init; }
    public Guid GroupId { get; init; }
    public string? GroupName { get; init; }
    public string? GroupCode { get; init; }
    public string? RegisteredByName { get; init; }
    public int MemberCount { get; init; }
    public string? Note { get; init; }
    public DateTime RegisteredAt { get; init; }
}
