namespace TEDF.API.Endpoints.DepartmentHeads;

public partial class DepartmentHeadEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/department-head").RequireAuthorization();

        MapQueryEndpoints(group);
        MapCommandEndpoints(group);
    }
}
