namespace TEDF.API.Endpoints.Topics.Requests;

public sealed class ProposeTopicRequest
{
    public string NameVi { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAbbr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Objectives { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public string? Technologies { get; set; }
    public string? ExpectedResults { get; set; }
    public int MaxStudents { get; set; } = 5;

    // The uploaded files are deliberately NOT properties here. Minimal API [FromForm] binding does
    // not populate a List<IFormFile> on a complex type — it silently leaves it null, which is how
    // the attachments on this endpoint came to be dropped in the first place. The handler reads them
    // off HttpContext.Request.Form.Files instead: the register form by the part name "registerForm",
    // and every other part as a supporting document.
}

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
