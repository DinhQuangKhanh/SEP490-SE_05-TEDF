using MediatR;
using TEDF.Application.Features.Settings.Queries.GetPublicSettings;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Settings;

/// <summary>
/// Anonymous endpoint the SPA fetches at startup to apply branding for ALL users and to learn the
/// maintenance state. Exposes no secrets. Must stay allowlisted in the maintenance middleware.
/// </summary>
public class GetPublicSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/public", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPublicSettingsQuery(), ct);
                return Ok(result);
            })
            .AllowAnonymous()
            .WithTags("Settings")
            .WithName("GetPublicSettings")
            .Produces(200);
    }
}
