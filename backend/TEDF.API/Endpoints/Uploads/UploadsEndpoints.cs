using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security.Validation;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Uploads;

/// <summary>
/// Synchronous single-file uploads that return a public URL immediately (no async malware scan).
/// Used by the rich-text registration-note editor for inline images and attachments, so the editor
/// can embed the returned URL right away. Format/size are still validated server-side.
/// </summary>
public sealed class UploadsEndpoints : IEndpoint
{
    private const long PerFileMaxBytes = 10 * 1024 * 1024; // 10 MB / file

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads").RequireAuthorization();

        group.MapPost("/note-attachment", UploadNoteAttachment)
            .WithTags("Uploads")
            .WithName("UploadNoteAttachment")
            .Produces<object>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithMetadata(new RequestSizeLimitAttribute(PerFileMaxBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = PerFileMaxBytes })
            .DisableAntiforgery();
    }

    private static async Task<IResult> UploadNoteAttachment(
        HttpContext httpContext,
        IFileStorageService fileStorage,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
            return Results.BadRequest(ApiResponse.Fail("Request phải là multipart/form-data."));

        var file = httpContext.Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return Results.BadRequest(ApiResponse.Fail("Không có tệp nào được gửi lên."));

        // Reuse the shared validator (extension whitelist + magic-byte signature + size).
        if (!FileUploadValidator.TryValidate([file], PerFileMaxBytes, maxAttachmentCount: 1, out var error))
            return Results.BadRequest(ApiResponse.Fail(error));

        await using var stream = file.OpenReadStream();
        var result = await fileStorage.UploadAsync(stream, file.FileName, "registration-notes", cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.PublicUrl))
            return Results.BadRequest(ApiResponse.Fail(result.Error ?? "Tải tệp lên thất bại."));

        return Ok(new
        {
            url = result.PublicUrl,
            originalFileName = file.FileName,
            fileSize = file.Length,
            contentType = file.ContentType,
        });
    }
}
