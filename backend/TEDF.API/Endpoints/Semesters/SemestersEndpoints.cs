using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Features.Semesters.Commands.CreateSemester;
using TEDF.Application.Features.Semesters.Commands.DeleteSemester;
using TEDF.Application.Features.Semesters.Commands.ImportEligibleMentors;
using TEDF.Application.Features.Semesters.Commands.ImportEligibleStudents;
using TEDF.Application.Features.Semesters.Commands.PublishSemesterRoster;
using TEDF.Application.Features.Semesters.Commands.RemoveEligibleMentors;
using TEDF.Application.Features.Semesters.Commands.RemoveEligibleStudents;
using TEDF.Application.Features.Semesters.Commands.UpdateEligibleMentorMajor;
using TEDF.Application.Features.Semesters.Commands.UpdateSemester;
using TEDF.Application.Features.Semesters.Queries.GetActiveSemester;
using TEDF.Application.Features.Semesters.Queries.GetAllSemesters;
using TEDF.Application.Features.Semesters.Queries.GetEligibleMentors;
using TEDF.Application.Features.Semesters.Queries.GetEligibleStudents;
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
        adminGroup.MapPost("/{id:int}/eligible-mentors/import", ImportEligibleMentors).DisableAntiforgery().WithTags("Semesters").WithName("ImportEligibleMentors").Produces(200).Produces(400).Produces(401);
        adminGroup.MapGet("/{id:int}/eligible-students", GetEligibleStudents).WithTags("Semesters").WithName("GetEligibleStudents").Produces(200).Produces(401).Produces(404);
        adminGroup.MapGet("/{id:int}/eligible-mentors", GetEligibleMentors).WithTags("Semesters").WithName("GetEligibleMentors").Produces(200).Produces(401).Produces(404);
        adminGroup.MapPut("/{id:int}/eligible-mentors/{mentorId:guid}/major", UpdateEligibleMentorMajor).WithTags("Semesters").WithName("UpdateEligibleMentorMajor").Produces(204).Produces(400).Produces(401).Produces(404);
        adminGroup.MapPost("/{id:int}/roster/publish", PublishRoster).WithTags("Semesters").WithName("PublishSemesterRoster").Produces(204).Produces(400).Produces(401).Produces(404);
        adminGroup.MapPost("/{id:int}/eligible-students/bulk-delete", RemoveEligibleStudents).WithTags("Semesters").WithName("RemoveEligibleStudents").Produces(204).Produces(400).Produces(401).Produces(404);
        adminGroup.MapPost("/{id:int}/eligible-mentors/bulk-delete", RemoveEligibleMentors).WithTags("Semesters").WithName("RemoveEligibleMentors").Produces(204).Produces(400).Produces(401).Produces(404);

        app.MapGet("/api/semesters/public", GetSemestersPublic).RequireAuthorization().WithTags("Semesters").WithName("GetSemestersPublic").Produces(200).Produces(401);
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

    private static async Task<IResult> ImportEligibleMentors(int id, IFormFile file, ISender sender, HttpContext context)
    {
        var userId = context.User.GetUserId();
        using var stream = file.OpenReadStream();
        var command = new ImportEligibleMentorsCommand(id, stream, file.FileName, userId);
        var result = await sender.Send(command);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEligibleStudents(int id, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEligibleStudentsQuery(id), ct));

    private static async Task<IResult> GetEligibleMentors(int id, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetEligibleMentorsQuery(id), ct));

    private static async Task<IResult> UpdateEligibleMentorMajor(
        int id, Guid mentorId, UpdateEligibleMentorMajorRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UpdateEligibleMentorMajorCommand(id, mentorId, body.MajorId), ct);
        return NoContent("Cập nhật ngành thành công.");
    }

    private static async Task<IResult> PublishRoster(int id, ISender sender, HttpContext context, CancellationToken ct)
    {
        var userId = context.User.GetUserId();
        await sender.Send(new PublishSemesterRosterCommand(id, userId), ct);
        return NoContent("Đã công bố danh sách và gửi thông báo.");
    }

    private static async Task<IResult> RemoveEligibleStudents(int id, RemoveEligibleEntriesRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RemoveEligibleStudentsCommand(id, body.Ids), ct);
        return NoContent("Đã xóa sinh viên khỏi danh sách.");
    }

    private static async Task<IResult> RemoveEligibleMentors(int id, RemoveEligibleEntriesRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RemoveEligibleMentorsCommand(id, body.Ids), ct);
        return NoContent("Đã xóa giảng viên khỏi danh sách.");
    }

    public record UpdateEligibleMentorMajorRequest(int MajorId);
    public record RemoveEligibleEntriesRequest(IReadOnlyList<Guid> Ids);

    private static async Task<IResult> GetSemestersPublic(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAllSemestersQuery(null), ct);
        return Ok(result);
    }
}
