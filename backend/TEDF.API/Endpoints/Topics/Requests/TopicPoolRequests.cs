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
    public List<IFormFile>? Attachments { get; set; }
}

public sealed record TopicRegistrationRequest(Guid ProjectId, string? Note);
public sealed record RejectTopicRegistrationRequest(string Reason);
