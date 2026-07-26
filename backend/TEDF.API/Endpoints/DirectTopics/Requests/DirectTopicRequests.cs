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
    // Optional: the student edit form does not change the member cap. When omitted the
    // existing value is preserved (a missing value must not overwrite it with the default 0).
    int? MaxStudents
);

public record MentorReviewRequest(string Action, string? Feedback);
