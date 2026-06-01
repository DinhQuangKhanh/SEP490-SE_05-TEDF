namespace TEDF.Application.Features.Mentor.DTOs;

public class MentorTopicItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameVi { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string MajorName { get; init; } = "";
    public int SourceType { get; init; } // 0=FromPool, 1=DirectRegistration
    public string SourceTypeName { get; init; } = "";
    public int Status { get; init; } // ProjectStatus enum value
    public string StatusName { get; init; } = "";
    public DateTime? SubmittedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string SemesterName { get; init; } = "";
}

public record GetMentorTopicsResult(
    IReadOnlyList<MentorTopicItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
