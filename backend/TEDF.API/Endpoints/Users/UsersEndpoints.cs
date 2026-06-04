using MediatR;
using TEDF.API.Endpoints.Users.Requests;
using TEDF.Application.Features.Departments.Commands.AssignDepartmentHead;
using TEDF.Application.Features.Users.Commands.LockUser;
using TEDF.Application.Features.Users.Commands.UnlockUser;
using TEDF.Application.Features.Users.Queries.GetUsers;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Users;

public sealed class UsersEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/users").RequireAuthorization(PolicyNames.RequireAdmin);

        adminGroup.MapGet("", GetUsers)
            .WithTags("Users").WithName("GetUsers")
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
    }

    private static async Task<IResult> GetUsers(ISender sender, string? role, string? search, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => Ok(await sender.Send(new GetUsersQuery(role, search, page, pageSize), ct));

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
}
