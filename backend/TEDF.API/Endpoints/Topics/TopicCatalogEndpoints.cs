using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Validation;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.Topics.DTOs;
using TEDF.Application.Features.Topics.Queries.GetMentorTopics;
using TEDF.Application.Features.Topics.Queries.GetTopicDetail;
using TEDF.Application.Features.Topics.Queries.GetTopicsInPool;
using TEDF.Domain.Enums.Document;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.Topics;

public sealed class TopicCatalogEndpoints : IEndpoint
{
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB
    private const long PerFileMaxBytes = 10 * 1024 * 1024; // 10 MB / file
    private const int MaxAttachmentCount = 5;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics").RequireAuthorization();

        group.MapGet("", GetTopicsInPool)
            .WithTags("Topics")
            .WithName("GetTopicsInPool")
            .Produces(200).Produces(401);

        group.MapGet("/{topicId:guid}", GetTopicDetail)
            .WithTags("Topics")
            .WithName("GetTopicDetail")
            .Produces<TopicDetailDto>().Produces(401).Produces(404);

        group.MapGet("/{topicId:guid}/documents", GetTopicDocuments)
            .WithTags("Topics")
            .WithName("GetTopicDocuments")
            .Produces(200).Produces(401);

        group.MapPost("/{topicId:guid}/documents", UploadTopicDocument)
            .WithTags("Topics")
            .WithName("UploadTopicDocuments")
            .Produces<object>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxUploadBytes })
            .DisableAntiforgery()
            .RequireAuthorization();

        // Replace/attach a topic's capstone register form (phiếu đăng ký). Route uses {projectId}
        // so the MentorOfProject handler can resolve the project. Stored as a Proposal document
        // (viewable), scanned; it does NOT re-parse the roster (that only happens at proposal time).
        group.MapPost("/{projectId:guid}/register-form", UploadRegisterForm)
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("Topics")
            .WithName("UploadTopicRegisterForm")
            .Produces<object>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxUploadBytes })
            .DisableAntiforgery();

        // Topics owned by the current mentor (moved from the Mentor role folder).
        // Literal "mentor" never matches the {topicId:guid} route, so there is no conflict.
        group.MapGet("/mentor", GetMentorTopics)
            .RequireAuthorization(PolicyNames.RequireMentor)
            .WithTags("Topics").WithName("GetMentorTopics")
            .Produces<ApiResponse<GetMentorTopicsResult>>().Produces(401);
    }

    private static async Task<IResult> GetTopicsInPool(
        ISender sender,
        int? majorId = null,
        string? search = null,
        int? poolStatus = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetTopicsInPoolQuery(majorId, search, poolStatus, sortBy, page, pageSize), ct);
        return Ok(result);
    }

    private static async Task<IResult> GetMentorTopics(
        [FromQuery] int? semesterId,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender sender,
        CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;
        return Ok(await sender.Send(new GetMentorTopicsQuery(semesterId, search, page, pageSize), ct));
    }

    private static async Task<IResult> GetTopicDetail(ISender sender, Guid topicId, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetTopicDetailQuery(topicId), ct);
        return result is not null ? Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> GetTopicDocuments(
        Guid topicId,
        ITopicsQueryService queryService,
        CancellationToken ct)
    {
        var documents = await queryService.GetTopicDocumentsAsync(topicId, ct);
        return Ok(documents);
    }

    private static async Task<IResult> UploadTopicDocument(
        Guid topicId,
        HttpContext httpContext,
        [FromServices] IAttachmentScanWorkflow scanWorkflow,
        ICurrentUserService currentUser,
        ILogger<TopicCatalogEndpoints> logger,
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

    private static async Task<IResult> UploadRegisterForm(
        Guid projectId,
        IFormFile file,
        [FromServices] IMalwareScanner scanner,
        IFileStorageService storage,
        IProjectDocumentWriteService documentWriter,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        if (file is null || file.Length == 0)
            return Results.BadRequest(ApiResponse.Fail("Không có tệp nào được gửi lên."));

        if (!FileUploadValidator.IsAllowedRegisterFormExtension(file.FileName))
            return Results.BadRequest(ApiResponse.Fail("Phiếu đăng ký phải là PDF, DOC hoặc DOCX."));

        if (!FileUploadValidator.TryValidate([file], PerFileMaxBytes, maxAttachmentCount: 1, out var error))
            return Results.BadRequest(ApiResponse.Fail(error));

        // Synchronous: scan → store (only if clean) → commit the Proposal document, all in-request, so the
        // client's refetch shows it in ~1-2s. InsertDocumentAsync retires the previous register form (replace).
        // No roster parsing here (that is proposal-only).
        var result = await SynchronousAttachmentStore.ScanStoreAsync(
            scanner, storage, documentWriter, "registration-forms", projectId, userId.Value,
            DocumentType.Proposal, file, cancellationToken);

        return result.Status switch
        {
            SyncUploadStatus.Stored => Ok(new { stored = true }, "Đã tải lên phiếu đăng ký."),
            SyncUploadStatus.Infected => Results.BadRequest(
                ApiResponse.Fail(result.Message ?? "Tệp bị phát hiện chứa mã độc.")),
            SyncUploadStatus.ScannerUnavailable => Results.Json(
                ApiResponse.Fail(result.Message ?? "Dịch vụ quét mã độc tạm thời không khả dụng. Vui lòng thử lại sau."),
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.BadRequest(ApiResponse.Fail(result.Message ?? "Tải phiếu đăng ký thất bại.")),
        };
    }
}
