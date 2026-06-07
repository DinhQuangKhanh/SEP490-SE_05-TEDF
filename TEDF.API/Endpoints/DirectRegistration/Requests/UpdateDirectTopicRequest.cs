namespace TEDF.API.Endpoints.DirectRegistration.Requests;

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
