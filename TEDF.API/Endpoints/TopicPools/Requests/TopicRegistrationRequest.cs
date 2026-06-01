namespace TEDF.API.Endpoints.TopicPools.Requests;

public sealed record TopicRegistrationRequest(Guid ProjectId, string? Note);
