namespace TEDF.API.Endpoints.DirectTopics.Requests;

public record CreateDirectTopicRequest(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    Guid MentorId,
    int MajorId,
    int MaxStudents
);

public record UpdateDirectTopicRequest(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents
);

public record MentorReviewRequest(string Action, string? Feedback);
