using Microsoft.EntityFrameworkCore;
using TEDF.Persistence.SqlServer;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Majors;

public sealed class MajorsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/majors", GetMajors)
            .RequireAuthorization()
            .WithTags("Majors")
            .WithName("GetMajors")
            .Produces(200).Produces(401);
    }

    private static async Task<IResult> GetMajors(AppDbContext context, CancellationToken ct)
    {
        var majors = await context.Majors
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name, m.Code })
            .ToListAsync(ct);

        return Ok(majors);
    }
}
