using MediatR;
using TEDF.Application.Features.Settings.Queries.GetSystemSettings;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins.Settings;

public class GetSystemSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/settings", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSystemSettingsQuery(), ct);
                return Ok(result);
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Settings")
            .WithName("GetSystemSettings")
            .Produces(200)
            .Produces(401)
            .Produces(403);
    }
}
