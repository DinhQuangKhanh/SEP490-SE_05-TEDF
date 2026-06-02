using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.Application.Features.Settings.Commands.UpdateSystemSettings;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Settings;

public class UpdateSystemSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/settings", async (
                [FromBody] Dictionary<string, string> settings,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new UpdateSystemSettingsCommand(settings ?? new()), ct);
                return Ok("Đã lưu cấu hình hệ thống.");
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Settings")
            .WithName("UpdateSystemSettings")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
