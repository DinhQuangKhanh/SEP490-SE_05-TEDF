namespace TEDF.API.Endpoints.Topics.Requests;

// The propose endpoint takes no bound request model. Its multipart form carries the register form
// (and any supporting documents) as files plus an optional "note" text part; Minimal API [FromForm]
// binding to a complex type does not populate IFormFile lists AND yields a *null* model when the form
// omits every matched field (e.g. a blank note) — both silent failures. The handler reads everything
// off HttpContext.Request.Form directly: files via Form.Files, the note via Form["note"].

public sealed record TopicRegistrationRequest(Guid ProjectId, string? Note);
public sealed record RejectTopicRegistrationRequest(string Reason);

/// <summary>Mentor editing a pool topic (PUT /api/topic-pools/topics/{id}/update).</summary>
public sealed record MentorUpdatePoolTopicRequest(
    string NameVi,
    string NameEn,
    string NameAbbr,
    string Description,
    string Objectives,
    string? Scope,
    string? Technologies,
    string? ExpectedResults,
    int MaxStudents);
