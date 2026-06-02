using MediatR;
using Microsoft.AspNetCore.Http;
using TEDF.Application.Common;
using TEDF.Application.Features.Settings.Commands.UploadLogo;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Settings;

public class UploadLogoEndpoint : IEndpoint
{
    private const long MaxLogoBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/jpg", "image/svg+xml"];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/settings/logo", async (
                IFormFile file,
                ISender sender,
                CancellationToken ct) =>
            {
                if (file is null || file.Length == 0)
                    return Results.BadRequest(ApiResponse.Fail("Vui lòng chọn tệp logo."));
                if (file.Length > MaxLogoBytes)
                    return Results.BadRequest(ApiResponse.Fail("Logo không được vượt quá 2MB."));
                if (!AllowedContentTypes.Contains(file.ContentType))
                    return Results.BadRequest(ApiResponse.Fail("Định dạng logo phải là PNG, JPG hoặc SVG."));

                await using var stream = file.OpenReadStream();
                var url = await sender.Send(new UploadLogoCommand(stream, file.FileName, file.ContentType), ct);
                return Ok(new { logoUrl = url });
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .DisableAntiforgery()
            .WithTags("Settings")
            .WithName("UploadLogo")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
