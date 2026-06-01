using TEDF.API.Extensions;
using TEDF.Persistence.MongoDB.Repositories.Interfaces;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admin;

public class GetUserActivityLogsEndpoint : IEndpoint
{
  public void MapEndpoint(IEndpointRouteBuilder app)
  {
    app.MapGet("/api/admin/activity-logs", async (
            IUserActivityLogRepository repository,
            string? role,
            string? category,
            string? severity,
            string? search,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
          if (page < 1) page = 1;
          if (pageSize is < 1 or > 100) pageSize = 20;

          var (items, totalCount) = await repository.GetPagedAsync(
                  role, category, severity, search, from, to, page, pageSize, cancellationToken);

          var result = new
          {
            Items = items.Select(i => new
            {
              i.Id,
              UserId = i.UserId.ToString(),
              i.UserName,
              i.UserEmail,
              i.ActiveRole,
              i.Action,
              i.Category,
              i.EntityType,
              EntityId = i.EntityId?.ToString(),
              i.Severity,
              i.IpAddress,
              i.Timestamp,
            }),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
          };

          return Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Admin")
        .WithName("GetUserActivityLogs")
        .Produces(200)
        .Produces(401);
  }
}
