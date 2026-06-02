using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Validation;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Domain.Enums.Document;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Students.Topics;

public partial class TopicEndpoints : IEndpoint
{
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB
    private const long PerFileMaxBytes = 10 * 1024 * 1024; // 10 MB / file
    private const int MaxAttachmentCount = 5;

    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Commands: các endpoint làm thay đổi dữ liệu/state
        // ─────────────────────────────────────────────────────────────

        #region Tải lên tài liệu cho đề tài

        // POST /api/topics/{topicId}/documents
        // Sinh viên tải tài liệu lên đề tài/dự án của mình.
        // Sử dụng quy trình quarantine → quét mã độc → promote.
        group.MapPost("{topicId:guid}/documents", UploadTopicDocuments)
            .WithTags("Topics")
            .WithName("UploadTopicDocuments")
            .Produces<object>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxUploadBytes })
            .DisableAntiforgery();

        #endregion
    }

    #region Handler: tải lên tài liệu cho đề tài

    private static async Task<IResult> UploadTopicDocuments(
        Guid topicId,
        HttpContext httpContext,
        [FromServices] IAttachmentScanWorkflow scanWorkflow,
        ICurrentUserService currentUser,
        ILogger<TopicEndpoints> logger,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        var hasFormContentType = httpContext.Request.HasFormContentType;
        if (!hasFormContentType)
            return Results.BadRequest(ApiResponse.Fail("Request phải là multipart/form-data."));

        var files = httpContext.Request.Form.Files;
        if (files.Count == 0)
            return Results.BadRequest(ApiResponse.Fail("Không có tệp nào được gửi lên."));

        var effectiveAttachments = files.ToList() as IReadOnlyCollection<IFormFile>;

        logger.LogInformation(
            "UploadTopicDocuments: TopicId={TopicId}, UserId={UserId}, FileCount={FileCount}",
            topicId, userId, effectiveAttachments.Count);

        if (!FileUploadValidator.TryValidate(
                effectiveAttachments,
                perFileMaxBytes: PerFileMaxBytes,
                maxAttachmentCount: MaxAttachmentCount,
                out var attachmentError))
        {
            return Results.BadRequest(ApiResponse.Fail(attachmentError));
        }

        var scanContext = new AttachmentScanContext(
            FolderPrefix: "topic-documents",
            ProjectId: topicId,
            UploadedBy: userId.Value,
            FolderPartitionId: topicId,
            DocumentType: DocumentType.Report);

        var queueResult = await scanWorkflow.QueueAsync(scanContext, effectiveAttachments, cancellationToken);

        var message = queueResult.QueuedCount > 0
            ? $"Tải lên thành công. Có {queueResult.QueuedCount} tệp đang chờ quét mã độc trong nền."
            : queueResult.Success
                ? "Không có tệp nào để xử lý."
                : "Tải lên thất bại. Không thể đưa tệp vào hàng đợi quét mã độc.";

        if (!queueResult.Success)
            return Results.BadRequest(ApiResponse.Fail(queueResult.ErrorMessage ?? message));

        return Ok(new { queuedCount = queueResult.QueuedCount }, message);
    }

    #endregion
}
