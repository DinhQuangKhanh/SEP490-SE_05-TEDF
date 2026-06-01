using TEDF.API.Extensions;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class GetActivityLogErrorDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/activity-logs/errors", async (
                IUserActivityLogRepository repository,
                Guid userId,
                string action,
                DateTime? from,
                DateTime? to,
                CancellationToken cancellationToken = default) =>
            {
                var errors = await repository.GetErrorDetailsAsync(
                    userId, action, from, to, cancellationToken);

                return Ok(new
                {
                    Errors = errors.Select(e => new
                    {
                        e.Message,
                        e.ErrorType,
                        e.Count,
                        e.LatestAt,
                    })
                });
            })
            .RequireAuthorization()
            .WithTags("Admin")
            .WithName("GetActivityLogErrorDetails")
            .Produces(200)
            .Produces(401);
    }
}
