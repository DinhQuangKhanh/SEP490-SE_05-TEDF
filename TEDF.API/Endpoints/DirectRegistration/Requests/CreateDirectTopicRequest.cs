namespace TEDF.API.Endpoints.DirectRegistration.Requests;

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
