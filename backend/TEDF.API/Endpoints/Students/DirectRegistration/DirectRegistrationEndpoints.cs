namespace TEDF.API.Endpoints.Students.DirectRegistration;

public partial class DirectRegistrationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/student").RequireAuthorization();

        MapQueryEndpoints(group);
        MapCommandEndpoints(group);
    }
}
