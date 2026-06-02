namespace TEDF.API.Endpoints.Students.DirectRegistration.Requests;

#region Direct Registration Request DTOs

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

#endregion
