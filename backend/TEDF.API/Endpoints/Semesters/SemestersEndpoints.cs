using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Semesters.Commands.CreateSemester;
using TEDF.Application.Features.Semesters.Commands.DeleteSemester;
using TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;
using TEDF.Application.Features.Semesters.Commands.UpdateSemester;
using TEDF.Application.Features.Semesters.Queries.GetActiveSemester;
using TEDF.Application.Features.Semesters.Queries.GetAllSemesters;
using TEDF.Application.Features.Semesters.Queries.GetSemesterById;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Semesters;

public sealed class SemestersEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/semesters").RequireAuthorization(PolicyNames.RequireAdmin);

        adminGroup.MapGet("", GetAllSemesters).WithTags("Semesters").WithName("GetAllSemesters").Produces(200).Produces(401);
        adminGroup.MapGet("/{id:int}", GetSemesterById).WithTags("Semesters").WithName("GetSemesterById").Produces(200).Produces(401).Produces(404);
        adminGroup.MapGet("/active", GetActiveSemester).WithTags("Semesters").WithName("GetActiveSemester").Produces(200).Produces(401).Produces(404);
        adminGroup.MapPost("", CreateSemester).WithTags("Semesters").WithName("CreateSemester").Produces(201).Produces(400).Produces(401);
        adminGroup.MapPut("/{id:int}", UpdateSemester).WithTags("Semesters").WithName("UpdateSemester").Produces(204).Produces(400).Produces(401).Produces(404);
        adminGroup.MapDelete("/{id:int}", DeleteSemester).WithTags("Semesters").WithName("DeleteSemester").Produces(204).Produces(400).Produces(401).Produces(404);
        adminGroup.MapPost("/{id:int}/eligible-students/import", ImportEligibleStudents).DisableAntiforgery().WithTags("Semesters").WithName("ImportEligibleStudents").Produces(200).Produces(400).Produces(401);

        app.MapGet("/api/semesters", GetSemestersPublic).RequireAuthorization().WithTags("Semesters").WithName("GetSemestersPublic").Produces(200).Produces(401);
    }

    private static async Task<IResult> GetAllSemesters([FromQuery] string? status, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetAllSemestersQuery(status), ct));

    private static async Task<IResult> GetSemesterById(int id, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetSemesterByIdQuery(id), ct));

    private static async Task<IResult> GetActiveSemester(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveSemesterQuery(), ct);
        return result is null
            ? Results.NotFound(ApiResponse.Fail("Hiện không có học kỳ đang diễn ra."))
            : Ok(result);
    }

    private static async Task<IResult> CreateSemester(CreateSemesterCommand command, ISender sender, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Created($"/api/semesters/{id}", new { id }, "Tạo mới thành công.");
    }

    private static async Task<IResult> UpdateSemester(int id, UpdateSemesterCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(ApiResponse.Fail("Id trong đường dẫn không khớp với dữ liệu gửi lên."));
        await sender.Send(command, ct);
        return NoContent("Cập nhật thành công.");
    }

    private static async Task<IResult> DeleteSemester(int id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeleteSemesterCommand(id), ct);
        return NoContent("Xóa thành công.");
    }

    private static async Task<IResult> ImportEligibleStudents(int id, IFormFile file, ISender sender, HttpContext context)
    {
        var userId = context.User.GetUserId();
        using var stream = file.OpenReadStream();
        var command = new ImportEligibleStudentsCommand(id, stream, file.FileName, userId);
        var result = await sender.Send(command);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSemestersPublic(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllSemestersQuery(null), ct);
        return Ok(result);
    }
}
