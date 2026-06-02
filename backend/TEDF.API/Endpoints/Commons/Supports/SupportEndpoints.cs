namespace TEDF.API.Endpoints.Commons.Supports;

public partial class SupportEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/supports").RequireAuthorization();

        MapQueryEndpoints(group);
        MapCommandEndpoints(group);
    }
}
