using TEDF.Application.Common;
using TEDF.Domain.Common.Interfaces;
using TEDF.Domain.Entities;
using TEDF.Infrastructure.Authorization.Policies;

namespace TEDF.API.Endpoints.Admins.Archives;

public class DownloadProjectArchiveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/archives/{id:guid}/download", async (
                Guid id,
                IProjectArchiveRepository repository,
                IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                var archive = await repository.GetByIdAsync(id, ct);
                if (archive is null)
                    return Results.NotFound(ApiResponse.Fail("Không tìm thấy đề tài lưu trữ."));
                if (string.IsNullOrWhiteSpace(archive.DocumentUrl))
                    return Results.BadRequest(ApiResponse.Fail("Đề tài này chưa có tệp lưu trữ."));

                archive.IncrementDownloadCount();
                repository.Update(archive);
                await unitOfWork.SaveChangesAsync(ct);

                // Stored as a public URL → redirect the client to it.
                return Results.Redirect(archive.DocumentUrl);
            })
            .RequireAuthorization(PolicyNames.RequireAdmin)
            .WithTags("Archives")
            .WithName("DownloadProjectArchive")
            .Produces(302)
            .Produces(400)
            .Produces(404);
    }
}
