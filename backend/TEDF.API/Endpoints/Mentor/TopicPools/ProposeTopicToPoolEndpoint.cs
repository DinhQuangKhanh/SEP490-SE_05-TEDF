using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Validation;
using TEDF.API.Extensions;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;
using TEDF.Domain.Enums.Document;
using static TEDF.API.Extensions.ApiResponseExtensions;
using TEDF.API.Endpoints.Mentor.TopicPools.Requests;

namespace TEDF.API.Endpoints.Mentor.TopicPools;

/// <summary>
/// Endpoint: POST /api/topic-pools/{poolId}/propose
/// Allows a mentor to propose a new topic into a topic pool.
/// </summary>
public class ProposeTopicToPoolEndpoint : IEndpoint
{
    private const long MaxUploadBytes = 25 * 1024 * 1024; // 25 MB
    private const long PerFileMaxBytes = 10 * 1024 * 1024; // 10 MB / file
    private const int MaxAttachmentCount = 5;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/topic-pools/{poolId:guid}/propose", async (
                Guid poolId,
                [FromForm] ProposeTopicRequest body,
                HttpContext httpContext,
                [FromServices] IAttachmentScanWorkflow scanWorkflow,
                ICurrentUserService currentUser,
                ILogger<ProposeTopicToPoolEndpoint> logger,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var hasFormContentType = httpContext.Request.HasFormContentType;
                var requestFormFilesCount = hasFormContentType ? httpContext.Request.Form.Files.Count : 0;
                var modelAttachmentsCount = body.Attachments?.Count ?? 0;
                var effectiveAttachments = modelAttachmentsCount > 0
                    ? body.Attachments
                    : requestFormFilesCount > 0
                        ? [.. httpContext.Request.Form.Files]
                        : null;
                var effectiveAttachmentsCount = effectiveAttachments?.Count ?? 0;

                logger.LogInformation(
                    "ProposeTopic multipart diagnostics: HasFormContentType={HasFormContentType}, Request.Form.Files.Count={RequestFormFilesCount}, Body.Attachments.Count={ModelAttachmentsCount}, EffectiveAttachments.Count={EffectiveAttachmentsCount}",
                    hasFormContentType,
                    requestFormFilesCount,
                    modelAttachmentsCount,
                    effectiveAttachmentsCount);

                if (!FileUploadValidator.TryValidate(
                        effectiveAttachments,
                        perFileMaxBytes: PerFileMaxBytes,
                        maxAttachmentCount: MaxAttachmentCount,
                        out var attachmentError))
                {
                    return Results.BadRequest(ApiResponse.Fail(attachmentError));
                }

                var command = new ProposeTopicToPoolCommand(
                    PoolId: poolId,
                    NameVi: body.NameVi,
                    NameEn: body.NameEn,
                    NameAbbr: body.NameAbbr,
                    Description: body.Description,
                    Objectives: body.Objectives,
                    Scope: body.Scope,
                    Technologies: body.Technologies,
                    ExpectedResults: body.ExpectedResults,
                    MaxStudents: body.MaxStudents);

                var projectId = await sender.Send(command, cancellationToken);

                var scanContext = new AttachmentScanContext(
                    FolderPrefix: "topic-proposals",
                    ProjectId: projectId,
                    UploadedBy: currentUser.UserId ?? Guid.Empty,
                    FolderPartitionId: poolId,
                    DocumentType: DocumentType.Proposal,
                    ExtraMetadata: new Dictionary<string, object?> { ["poolId"] = poolId });

                var queueResult = await scanWorkflow.QueueAsync(scanContext, effectiveAttachments, cancellationToken);

                var message = queueResult.QueuedCount > 0
                    ? $"Tạo mới thành công. Có {queueResult.QueuedCount} tệp đang chờ quét mã độc trong nền."
                    : queueResult.Success
                        ? "Tạo mới thành công. Không có tệp đính kèm để quét."
                        : "Tạo mới thành công, nhưng chưa thể đưa tệp đính kèm vào hàng đợi quét mã độc.";

                return Created($"/api/topic-pools/topics/{projectId}", new { id = projectId }, message);
            })
            .WithTags("TopicPools")
            .WithName("ProposeTopicToPool")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxUploadBytes })
            .DisableAntiforgery()
            .WithRequestTimeout("ProposeTopicUploadTimeout")
            .RequireRateLimiting("ProposeTopicUploadPolicy")
            .RequireAuthorization();
    }
}
