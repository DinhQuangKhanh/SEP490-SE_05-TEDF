namespace TEDF.API.Endpoints.Students.Topics;

public partial class TopicEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/topics").RequireAuthorization();

        MapQueryEndpoints(group);
        MapCommandEndpoints(group);
    }
}
