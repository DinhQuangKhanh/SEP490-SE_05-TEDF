using MediatR;
using TEDF.Application.Common;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Application.Features.Archives.Queries.GetProjectArchives;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Archives;

public sealed class ArchivesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/archives")
            .RequireAuthorization(PolicyNames.RequireAdmin);

        adminGroup.MapGet("", GetProjectArchives)
            .WithTags("Archives")
            .WithName("GetProjectArchives")
            .Produces(200).Produces(401).Produces(403);

        adminGroup.MapGet("/{id:guid}/download", DownloadProjectArchive)
            .WithTags("Archives")
            .WithName("DownloadProjectArchive")
            .Produces(302).Produces(400).Produces(404);
    }

    private static async Task<IResult> GetProjectArchives(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectArchivesQuery(), ct));

    private static async Task<IResult> DownloadProjectArchive(
        Guid id, IProjectArchiveRepository repository, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var archive = await repository.GetByIdAsync(id, ct);
        if (archive is null)
            return Results.NotFound(ApiResponse.Fail("Không tìm thấy đề tài lưu trữ."));
        if (string.IsNullOrWhiteSpace(archive.DocumentUrl))
            return Results.BadRequest(ApiResponse.Fail("Đề tài này chưa có tệp lưu trữ."));

        archive.IncrementDownloadCount();
        repository.Update(archive);
        await unitOfWork.SaveChangesAsync(ct);

        return Results.Redirect(archive.DocumentUrl);
    }
}
