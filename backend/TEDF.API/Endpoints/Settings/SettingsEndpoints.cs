using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Common;
using TEDF.Application.Features.Settings.Commands.SendTestEmail;
using TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;
using TEDF.Application.Features.Settings.Commands.UploadLogo;
using TEDF.Application.Features.Settings.Queries.GetPublicSettings;
using TEDF.Application.Features.Settings.Queries.GetSystemSettings;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Settings;

public sealed class SettingsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup("/api/settings");
        publicGroup.MapGet("/public", GetPublicSettings)
            .AllowAnonymous()
            .WithTags("Settings")
            .WithName("GetPublicSettings")
            .Produces(200);

        var adminGroup = app.MapGroup("/api/settings")
            .RequireAuthorization(PolicyNames.RequireAdmin);

        adminGroup.MapGet("", GetSystemSettings)
            .WithTags("Settings")
            .WithName("GetSystemSettings")
            .Produces(200).Produces(401).Produces(403);

        adminGroup.MapPut("", UpdateSystemSettings)
            .WithTags("Settings")
            .WithName("UpdateSystemSettings")
            .Produces(200).Produces(400).Produces(401).Produces(403);

        adminGroup.MapPost("/test-email", SendTestEmail)
            .WithTags("Settings")
            .WithName("SendTestEmail")
            .Produces(200).Produces(400).Produces(401).Produces(403);

        adminGroup.MapPost("/logo", UploadLogo)
            .DisableAntiforgery()
            .WithTags("Settings")
            .WithName("UploadLogo")
            .Produces(200).Produces(400).Produces(401).Produces(403);
    }

    private static async Task<IResult> GetPublicSettings(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicSettingsQuery(), ct);
        return Ok(result);
    }

    private static async Task<IResult> GetSystemSettings(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetSystemSettingsQuery(), ct);
        return Ok(result);
    }

    private static async Task<IResult> UpdateSystemSettings(
        [FromBody] Dictionary<string, string> settings,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new UpdateSystemSettingsCommand(settings ?? new()), ct);
        return Ok("Đã lưu cấu hình hệ thống.");
    }

    private static async Task<IResult> SendTestEmail(ISender sender, CancellationToken ct)
    {
        await sender.Send(new SendTestEmailCommand(), ct);
        return Ok("Đã gửi email kiểm tra.");
    }

    private const long MaxLogoBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/jpg", "image/svg+xml"];

    private static async Task<IResult> UploadLogo(IFormFile file, ISender sender, CancellationToken ct)
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
    }
}
