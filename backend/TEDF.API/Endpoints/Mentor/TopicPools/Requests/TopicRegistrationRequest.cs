namespace TEDF.API.Endpoints.Mentor.TopicPools.Requests;

public sealed record TopicRegistrationRequest(Guid ProjectId, string? Note);
