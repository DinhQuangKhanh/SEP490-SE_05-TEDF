using MediatR;
using TEDF.Application.Features.Settings.Commands.SendTestEmail;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Admins.Settings;

public class SendTestEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/settings/test-email", async (ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new SendTestEmailCommand(), ct);
                return Ok("Đã gửi email kiểm tra.");
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Settings")
            .WithName("SendTestEmail")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);
    }
}
