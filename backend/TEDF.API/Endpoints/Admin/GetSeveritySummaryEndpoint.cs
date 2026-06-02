using TEDF.API.Extensions;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class GetSeveritySummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/activity-logs/severity-summary", async (
                IUserActivityLogRepository repository,
                string? role,
                DateTime? from,
                DateTime? to,
                CancellationToken cancellationToken = default) =>
            {
                var summary = await repository.GetSeveritySummaryAsync(
                    role, from, to, cancellationToken);

                return Ok(new
                {
                    summary.Info,
                    summary.Warning,
                    summary.Error,
                    summary.Critical,
                    Total = summary.Info + summary.Warning + summary.Error + summary.Critical,
                });
            })
            .RequireAuthorization()
            .WithTags("Admin")
            .WithName("GetSeveritySummary")
            .Produces(200)
            .Produces(401);
    }
}
