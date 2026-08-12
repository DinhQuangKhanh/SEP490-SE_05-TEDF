using MediatR;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Application.Features.TopicPools.Commands.CancelRegistration;
using TEDF.Application.Features.TopicPools.Commands.ConfirmRegistration;
using TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;
using TEDF.Application.Features.TopicPools.Commands.RejectRegistration;
using TEDF.Application.Features.TopicPools.Commands.RequestRegistration;
using TEDF.Application.Features.TopicPools.Queries.GetGroupRegistrations;
using TEDF.Application.Features.TopicPools.Queries.GetMentorRegistrations;
using TEDF.Application.Features.TopicPools.Queries.GetProjectRegistration;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolById;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPools;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolsByDepartment;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolStatistics;
using TEDF.API.Endpoints.Topics.Requests;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Validation;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.Commands.MentorResubmitPoolTopic;
using TEDF.Application.Features.TopicPools.Commands.MentorUpdatePoolTopic;
using TEDF.Domain.Enums.Document;

namespace TEDF.API.Endpoints.Topics;

public sealed class TopicPoolsEndpoints : IEndpoint
{
    private const long NoteAttachmentMaxBytes = 10 * 1024 * 1024; // 10 MB / file

    // Propose-topic upload budget: the register form plus up to five supporting documents.
    private const long ProposalUploadMaxBytes = 25 * 1024 * 1024;
    private const long PerFileMaxBytes = 10 * 1024 * 1024;
    private const int MaxAttachmentCount = 6;

    private static readonly string[] RegisterFormExtensions = [".pdf", ".docx"];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var pool = app.MapGroup("/api/topic-pools").RequireAuthorization();

        // Synchronous single-file upload for the registration-note editor (inline images + attachments);
        // returns a public URL immediately. Format/size validated server-side; no async malware scan.
        pool.MapPost("/note-attachment", UploadNoteAttachment)
            .WithTags("TopicPools")
            .WithName("UploadRegistrationNoteAttachment")
            .Produces<object>()
            .Produces(400).Produces(401)
            .WithMetadata(new RequestSizeLimitAttribute(NoteAttachmentMaxBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = NoteAttachmentMaxBytes })
            .DisableAntiforgery();

        pool.MapGet("", GetTopicPools)
            .WithTags("TopicPools")
            .WithName("GetTopicPools")
            .Produces<List<TopicPoolDto>>()
            .Produces(401);

        pool.MapGet("/{id:guid}", GetTopicPoolById)
            .WithTags("TopicPools")
            .WithName("GetTopicPoolById")
            .Produces<TopicPoolDto>()
            .Produces(401).Produces(404);

        pool.MapGet("/by-department", GetTopicPoolsByDepartment)
            .WithTags("TopicPools")
            .WithName("GetTopicPoolsByDepartment")
            .Produces<List<DepartmentWithPoolsDto>>()
            .Produces(401);

        pool.MapGet("/{id:guid}/statistics", GetTopicPoolStatistics)
            .WithTags("TopicPools")
            .WithName("GetTopicPoolStatistics")
            .Produces<TopicPoolStatisticsDto>()
            .Produces(401).Produces(404);

        // Carries the register form plus up to five supporting documents, so it needs its own body
        // limit. The rate limiter is defined in ServiceCollectionExtensions; this is the route that
        // opts into it. No request-timeout policy on purpose: aborting mid-request could kill the
        // call between "project committed" and "attachments queued", which the 201-with-warning
        // response below is specifically designed to avoid.
        pool.MapPost("/{poolId:guid}/propose", ProposeTopicToPool)
            .RequireAuthorization(PolicyNames.RequireMentor)
            .DisableAntiforgery()
            .WithTags("TopicPools")
            .WithName("ProposeTopicToPool")
            .WithMetadata(new RequestSizeLimitAttribute(ProposalUploadMaxBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = ProposalUploadMaxBytes })
            .RequireRateLimiting("ProposeTopicUploadPolicy")
            .Produces(201).Produces(400).Produces(401).Produces(403).Produces(404).Produces(429).Produces(503);

        pool.MapPost("/{groupId:guid}/topic-registrations", RequestTopicRegistration)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("TopicPools")
            .WithName("RequestTopicRegistration")
            .Produces(201).Produces(400).Produces(401).Produces(403);

        pool.MapGet("/groups/{groupId:guid}/registrations", GetGroupRegistrations)
            .RequireAuthorization(PolicyNames.GroupMember)
            .WithTags("TopicPools")
            .WithName("GetGroupRegistrations")
            .Produces<List<GroupRegistrationDto>>()
            .Produces(401).Produces(403);

        // Supervising mentor views the confirmed registration (reason + attachments) of their group.
        pool.MapGet("/projects/{projectId:guid}/registration", GetProjectRegistration)
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("TopicPools")
            .WithName("GetProjectRegistration")
            .Produces<GroupRegistrationDto>()
            .Produces(401).Produces(403).Produces(404);

        pool.MapGet("/registrations/mentor", GetMentorRegistrations)
            .WithTags("TopicPools")
            .WithName("GetMentorRegistrations")
            .Produces<List<MentorRegistrationRequestDto>>()
            .Produces(401);

        pool.MapPut("/registrations/{id:guid}/cancel", CancelTopicRegistration)
            .WithTags("TopicPools")
            .WithName("CancelTopicRegistration")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        pool.MapPut("/registrations/{id:guid}/confirm", ConfirmTopicRegistration)
            .WithTags("TopicPools")
            .WithName("ConfirmTopicRegistration")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        pool.MapPut("/registrations/{id:guid}/reject", RejectTopicRegistration)
            .WithTags("TopicPools")
            .WithName("RejectTopicRegistration")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        // Mentor edits to a pool topic (moved from the Mentor role folder).
        pool.MapPut("/topics/{projectId:guid}/update", MentorUpdatePoolTopic)
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("TopicPools").WithName("MentorUpdatePoolTopic")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        pool.MapPut("/topics/{projectId:guid}/resubmit", MentorResubmitPoolTopic)
            .RequireAuthorization(PolicyNames.MentorOfProject)
            .WithTags("TopicPools").WithName("MentorResubmitPoolTopic")
            .Produces(200).Produces(400).Produces(401).Produces(404);
    }

    private static async Task<IResult> GetTopicPools(ISender sender, int? majorId = null, CancellationToken ct = default)
        => Ok(await sender.Send(new GetTopicPoolsQuery(majorId), ct));

    private static async Task<IResult> GetTopicPoolById(ISender sender, Guid id, CancellationToken ct = default)
        => Ok(await sender.Send(new GetTopicPoolByIdQuery(id), ct));

    private static async Task<IResult> GetTopicPoolsByDepartment(ISender sender, CancellationToken ct = default)
        => Ok(await sender.Send(new GetTopicPoolsByDepartmentQuery(), ct));

    private static async Task<IResult> GetTopicPoolStatistics(ISender sender, Guid id, CancellationToken ct = default)
        => Ok(await sender.Send(new GetTopicPoolStatisticsQuery(id), ct));

    private static async Task<IResult> ProposeTopicToPool(
        Guid poolId,
        [FromForm] ProposeTopicRequest body,
        [FromServices] IAttachmentScanWorkflow scanWorkflow,
        [FromServices] IMalwareScanner malwareScanner,
        ICurrentUserService currentUser,
        ILogger<TopicPoolsEndpoints> logger,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Results.Unauthorized();

        if (body.RegisterForm is not { Length: > 0 } registerForm)
            return Results.BadRequest(ApiResponse.Fail("Vui lòng tải lên phiếu đăng ký (Capstone Project Register)."));

        var attachments = body.Attachments.Where(f => f.Length > 0).ToList();
        var allFiles = attachments.Prepend(registerForm).ToList();

        if (!FileUploadValidator.TryValidate(allFiles, PerFileMaxBytes, MaxAttachmentCount, out var uploadError))
            return Results.BadRequest(ApiResponse.Fail(uploadError));

        var registerFormExtension = Path.GetExtension(registerForm.FileName);
        if (!RegisterFormExtensions.Contains(registerFormExtension, StringComparer.OrdinalIgnoreCase))
            return Results.BadRequest(ApiResponse.Fail("Phiếu đăng ký phải là tệp PDF hoặc DOCX."));

        // Scanned inline rather than through the background queue, because the roster is parsed from
        // this file in-process a few lines below — the content must be proven clean first. The queue
        // below scans it a second time; that duplication is deliberate, not an oversight.
        var scan = await malwareScanner.ScanAsync([registerForm], cancellationToken);
        if (scan.ScannerUnavailable)
            return Results.Json(ApiResponse.Fail(scan.Message), statusCode: StatusCodes.Status503ServiceUnavailable);

        if (!scan.IsClean)
            return Results.BadRequest(ApiResponse.Fail(scan.Message));

        using var registerFormStream = new MemoryStream();
        await registerForm.CopyToAsync(registerFormStream, cancellationToken);

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
            RegisterForm: registerFormStream.ToArray(),
            MaxStudents: body.MaxStudents
        );

        // The project must be committed before the files are queued: the scan job ends by inserting
        // a document row keyed on this project id.
        var projectId = await sender.Send(command, cancellationToken);

        var queued = await QueueProposalAttachmentsAsync(
            scanWorkflow, logger, projectId, userId.Value, registerForm, attachments, cancellationToken);

        // Deliberately still a 201 when queueing failed. The project is already committed and cannot
        // be rolled back here; reporting failure would push the mentor to propose the topic again and
        // create a duplicate. The roster was read from the form above, so only the archived copy is
        // at stake.
        var message = queued.AllQueued
            ? "Đề xuất đề tài thành công."
            : "Đề xuất đề tài thành công. Một số tệp đính kèm chưa được đưa vào hàng đợi quét mã độc, "
              + "vui lòng tải lên lại ở trang chi tiết đề tài.";

        return Created(
            $"/api/topic-pools/topics/{projectId}",
            new { id = projectId, queuedAttachments = queued.Count, attachmentWarning = !queued.AllQueued },
            message);
    }

    /// <summary>
    /// Sends the uploaded files through the quarantine → scan → promote pipeline. Two passes, because
    /// <see cref="AttachmentScanContext"/> carries a single document type and the register form has to
    /// stay identifiable as <see cref="DocumentType.Proposal"/> afterwards.
    /// </summary>
    private static async Task<(int Count, bool AllQueued)> QueueProposalAttachmentsAsync(
        IAttachmentScanWorkflow scanWorkflow,
        ILogger logger,
        Guid projectId,
        Guid userId,
        IFormFile registerForm,
        IReadOnlyCollection<IFormFile> attachments,
        CancellationToken cancellationToken)
    {
        AttachmentScanContext ContextFor(DocumentType documentType) => new(
            FolderPrefix: "topic-proposals",
            ProjectId: projectId,
            UploadedBy: userId,
            FolderPartitionId: projectId,
            DocumentType: documentType);

        var failures = new List<string>();
        var queuedCount = 0;

        var registerFormResult = await scanWorkflow.QueueAsync(
            ContextFor(DocumentType.Proposal), [registerForm], cancellationToken);

        queuedCount += registerFormResult.QueuedCount;
        if (!registerFormResult.Success)
            failures.Add($"phiếu đăng ký: {registerFormResult.ErrorMessage}");

        if (attachments.Count > 0)
        {
            var attachmentsResult = await scanWorkflow.QueueAsync(
                ContextFor(DocumentType.Other), attachments, cancellationToken);

            queuedCount += attachmentsResult.QueuedCount;
            if (!attachmentsResult.Success)
                failures.Add($"tài liệu đính kèm: {attachmentsResult.ErrorMessage}");
        }

        if (failures.Count > 0)
        {
            logger.LogError(
                "Propose topic {ProjectId}: could not queue every upload for scanning — {Failures}",
                projectId, string.Join("; ", failures));
        }

        return (queuedCount, failures.Count == 0);
    }

    private static async Task<IResult> RequestTopicRegistration(Guid groupId, TopicRegistrationRequest body, ISender sender, CancellationToken ct)
    {
        var registrationId = await sender.Send(new RequestTopicRegistrationCommand(body.ProjectId, groupId, body.Note), ct);
        return Created($"/api/topic-pools/registrations/{registrationId}", new { id = registrationId }, "Tạo mới thành công.");
    }

    private static async Task<IResult> GetGroupRegistrations(Guid groupId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetGroupRegistrationsQuery(groupId), ct));

    private static async Task<IResult> GetProjectRegistration(Guid projectId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectRegistrationQuery(projectId), ct));

    private static async Task<IResult> GetMentorRegistrations(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetMentorRegistrationsQuery(), ct));

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

        if (!FileUploadValidator.TryValidate([file], NoteAttachmentMaxBytes, maxAttachmentCount: 1, out var error))
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

    private static async Task<IResult> CancelTopicRegistration(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new CancelTopicRegistrationCommand(id), ct);
        return NoContent("Huỷ đăng ký thành công.");
    }

    private static async Task<IResult> ConfirmTopicRegistration(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new ConfirmTopicRegistrationCommand(id), ct);
        return NoContent("Xác nhận thành công.");
    }

    private static async Task<IResult> RejectTopicRegistration(Guid id, [FromBody] RejectTopicRegistrationRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new RejectTopicRegistrationCommand(id, body.Reason), ct);
        return NoContent("Từ chối yêu cầu thành công.");
    }

    private static async Task<IResult> MentorUpdatePoolTopic(
        Guid projectId, [FromBody] MentorUpdatePoolTopicRequest request, ISender sender, CancellationToken ct)
    {
        var command = new MentorUpdatePoolTopicCommand(
            projectId, request.NameVi, request.NameEn, request.NameAbbr, request.Description,
            request.Objectives, request.Scope, request.Technologies, request.ExpectedResults, request.MaxStudents);
        await sender.Send(command, ct);
        return NoContent("Cập nhật đề tài thành công.");
    }

    private static async Task<IResult> MentorResubmitPoolTopic(Guid projectId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new MentorResubmitPoolTopicCommand(projectId), ct);
        return Ok("Đã gửi đề tài đi thẩm định thành công.");
    }
}
