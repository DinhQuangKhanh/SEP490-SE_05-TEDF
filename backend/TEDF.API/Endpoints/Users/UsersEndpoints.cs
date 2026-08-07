using MediatR;
using TEDF.API.Endpoints.Users.Requests;
using TEDF.API.Extensions;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Users.Commands.AssignDepartmentHead;
using TEDF.Application.Features.Users.Commands.CreateUser;
using TEDF.Application.Features.Users.Commands.ImportUsers;
using TEDF.Application.Features.Users.Commands.LockUser;
using TEDF.Application.Features.Users.Commands.RevokeDepartmentHead;
using TEDF.Application.Features.Users.Commands.SetDepartmentHead;
using TEDF.Application.Features.Users.Commands.UnlockUser;
using TEDF.Application.Features.Users.Queries.GetUsers;
using TEDF.Application.Features.Users.Queries.GetMyProfile;
using TEDF.Application.Features.Users.Commands.UpdateMyProfile;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Users;

public sealed class UsersEndpoints : IEndpoint
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var usersGroup = app.MapGroup("/api/users").RequireAuthorization();

        usersGroup.MapGet("/me", GetMyProfile)
            .WithTags("Users").WithName("GetMyProfile")
            .Produces(200).Produces(401);

        usersGroup.MapPut("/me", UpdateMyProfile)
            .WithTags("Users").WithName("UpdateMyProfile")
            .Produces(204).Produces(400).Produces(401);

        var adminGroup = app.MapGroup("/api/users").RequireAuthorization(PolicyNames.RequireAdmin);

        adminGroup.MapGet("", GetUsers)
            .WithTags("Users").WithName("GetUsers")
            .Produces(200).Produces(401);

        adminGroup.MapPost("", CreateUser)
            .WithTags("Users").WithName("CreateUser")
            .Produces(201).Produces(400).Produces(401);

        adminGroup.MapPost("/import", ImportUsers)
            .DisableAntiforgery()
            .WithTags("Users").WithName("ImportUsers")
            .Produces(200).Produces(400).Produces(401);

        adminGroup.MapGet("/import/template", DownloadUserImportTemplate)
            .WithTags("Users").WithName("DownloadUserImportTemplate")
            .Produces(200).Produces(401);

        adminGroup.MapPut("/{userId:guid}/lock", LockUser)
            .WithTags("Users").WithName("LockUser")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        adminGroup.MapPut("/{userId:guid}/unlock", UnlockUser)
            .WithTags("Users").WithName("UnlockUser")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        // Assign a user as head of a department (moved from the role-based Admin/Departments folder).
        adminGroup.MapPost("/departments/{departmentId:int}/head", AssignDepartmentHead)
            .WithTags("Users").WithName("AssignDepartmentHead")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        // Grant / revoke the Department Head role straight from the user-management screen: the
        // department comes from the lecturer's own profile, so the caller only supplies the user.
        adminGroup.MapPost("/{userId:guid}/department-head", SetDepartmentHead)
            .WithTags("Users").WithName("SetDepartmentHead")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        adminGroup.MapDelete("/{userId:guid}/department-head", RevokeDepartmentHead)
            .WithTags("Users").WithName("RevokeDepartmentHead")
            .Produces(204).Produces(400).Produces(401).Produces(404);
    }

    private static async Task<IResult> GetUsers(ISender sender, string? role, string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => Ok(await sender.Send(new GetUsersQuery(role, search, page, pageSize), ct));

    private static async Task<IResult> CreateUser(CreateUserRequest request, ISender sender, CancellationToken ct)
    {
        var id = await sender.Send(new CreateUserCommand(
            request.Role, request.Email, request.FullName, request.Code,
            request.Phone, request.AcademicTitle, request.MajorId), ct);
        return Created($"/api/users/{id}", new { id }, "Tạo người dùng thành công.");
    }

    private static async Task<IResult> ImportUsers(IFormFile file, ISender sender, HttpContext context, CancellationToken ct)
    {
        var userId = context.User.GetUserId();
        using var stream = file.OpenReadStream();
        var result = await sender.Send(new ImportUsersCommand(stream, file.FileName, userId), ct);
        return Results.Ok(result);
    }

    private static IResult DownloadUserImportTemplate(IExcelService excel)
        => Results.File(excel.GenerateUserImportTemplate(), XlsxContentType, "danh_sach_nguoi_dung_mau.xlsx");

    private static async Task<IResult> GetMyProfile(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMyProfileQuery(), ct));

    private static async Task<IResult> UpdateMyProfile(UpdateMyProfileRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UpdateMyProfileCommand(request.PhoneNumber, request.BirthDate, request.PrivacySettings), ct);
        return NoContent("Cập nhật thông tin thành công.");
    }

    private static async Task<IResult> LockUser(Guid userId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new LockUserCommand(userId), ct);
        return NoContent("Khóa thành công.");
    }

    private static async Task<IResult> UnlockUser(Guid userId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UnlockUserCommand(userId), ct);
        return NoContent("Mở khóa thành công.");
    }

    private static async Task<IResult> AssignDepartmentHead(
        int departmentId, AssignDepartmentHeadRequest request, ISender sender, CancellationToken ct)
    {
        await sender.Send(new AssignDepartmentHeadCommand(departmentId, request.UserId), ct);
        return NoContent("Thiết lập trưởng bộ phận thành công.");
    }

    private static async Task<IResult> SetDepartmentHead(Guid userId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new SetDepartmentHeadCommand(userId), ct);
        return NoContent("Đã gán vai trò Trưởng bộ môn.");
    }

    private static async Task<IResult> RevokeDepartmentHead(Guid userId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RevokeDepartmentHeadCommand(userId), ct);
        return NoContent("Đã thu hồi vai trò Trưởng bộ môn.");
    }
}
