namespace TEDF.API.Endpoints.Students.StudentGroups;

public partial class StudentGroupEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/student-groups").RequireAuthorization();

        MapQueryEndpoints(group);
        MapCommandEndpoints(group);
    }
}
