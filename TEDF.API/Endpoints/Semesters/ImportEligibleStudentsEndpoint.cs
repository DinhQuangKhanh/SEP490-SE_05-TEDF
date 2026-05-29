using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TEDF.API.Extensions;
using TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;

namespace TEDF.API.Endpoints.Semesters;

public static class ImportEligibleStudentsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/admin/semesters/{id:int}/eligible-students/import", async (
            int id,
            IFormFile file,
            ISender sender,
            HttpContext context) =>
        {
            var userId = context.User.GetUserId();
            using var stream = file.OpenReadStream();
            var command = new ImportEligibleStudentsCommand(id, stream, file.FileName, userId);
            
            var result = await sender.Send(command);

            return Results.Ok(result);
        })
        .RequireAuthorization("RequireAdmin")
        .DisableAntiforgery()
        .WithTags("Semesters");
    }
}
