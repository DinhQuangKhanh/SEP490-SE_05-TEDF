using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Semesters.Commands.CreateSemester;
using TEDF.Application.Features.Semesters.Commands.DeleteSemester;
using TEDF.Application.Features.Semesters.Commands.UpdateSemester;
using TEDF.Application.Features.Semesters.Queries.GetActiveSemester;
using TEDF.Application.Features.Semesters.Queries.GetAllSemesters;
using TEDF.Application.Features.Semesters.Queries.GetSemesterById;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Semesters;

public class GetAllSemestersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/semesters", async (
                [FromQuery] string? status,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetAllSemestersQuery(status), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("GetAllSemesters")
            .Produces(200)
            .Produces(401);
    }
}

public class GetSemesterByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/semesters/{id:int}", async (
                int id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetSemesterByIdQuery(id), cancellationToken);
                return Ok(result);
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("GetSemesterById")
            .Produces(200)
            .Produces(401)
            .Produces(404);
    }
}

public class GetActiveSemesterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/semesters/active", async (
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetActiveSemesterQuery(), cancellationToken);
                return result is null
                    ? Results.NotFound(ApiResponse.Fail("Hiện không có học kỳ đang diễn ra."))
                    : Ok(result);
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("GetActiveSemester")
            .Produces(200)
            .Produces(401)
            .Produces(404);
    }
}

public class CreateSemesterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/semesters", async (
                CreateSemesterCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var id = await sender.Send(command, cancellationToken);
                return Created($"/api/admin/semesters/{id}", new { id }, "Tạo mới thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("CreateSemester")
            .Produces(201)
            .Produces(400)
            .Produces(401);
    }
}


public class UpdateSemesterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/semesters/{id:int}", async (
                int id,
                UpdateSemesterCommand command,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                if (id != command.Id)
                    return Results.BadRequest(ApiResponse.Fail("Id trong đường dẫn không khớp với dữ liệu gửi lên."));
                await sender.Send(command, cancellationToken);
                return NoContent("Cập nhật thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("UpdateSemester")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}

public class DeleteSemesterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/admin/semesters/{id:int}", async (
                int id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteSemesterCommand(id), cancellationToken);
                return NoContent("Xóa thành công.");
            })
            .RequireAuthorization("RequireAdmin")
            .WithTags("Semesters")
            .WithName("DeleteSemester")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
