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
using TEDF.API.Common.Security;
using TEDF.API.Common.Security.Abstractions;
using TEDF.API.Common.Security.Validation;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.TopicPools.Commands.MentorResubmitPoolTopic;
using TEDF.Application.Features.TopicPools.Commands.MentorUpdatePoolTopic;

namespace TEDF.API.Endpoints.Topics;

public sealed class TopicPoolsEndpoints : IEndpoint
{
    private const long NoteAttachmentMaxBytes = 10 * 1024 * 1024; // 10 MB / file
    private const int MaxProposalAttachments = 5;                  // free attachments per proposal
    private const long ProposalMaxBytes = 60 * 1024 * 1024;       // 5×10MB attachments + register form

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

        pool.MapPost("/{poolId:guid}/propose", ProposeTopicToPool)
            .RequireAuthorization(PolicyNames.RequireMentor)
            .DisableAntiforgery()
            .WithTags("TopicPools")
            .WithName("ProposeTopicToPool")
            .WithMetadata(new RequestSizeLimitAttribute(ProposalMaxBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = ProposalMaxBytes })
            .Produces(201).Produces(400).Produces(401).Produces(403).Produces(404).Produces(503);

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
        HttpContext httpContext,
        [FromServices] IAttachmentScanWorkflow scanWorkflow,
        ICurrentUserService currentUser,
        ILogger<TopicPoolsEndpoints> logger,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Validate every upload BEFORE creating the topic, so a bad file fails fast (no orphan project).
        byte[]? registerFormPdf = null;
        if (body.RegisterForm is { Length: > 0 } registerForm)
        {
            if (!FileUploadValidator.TryValidate([registerForm], NoteAttachmentMaxBytes, maxAttachmentCount: 1, out var registerFormError))
                return Results.BadRequest(ApiResponse.Fail(registerFormError));

            if (!FileUploadValidator.IsAllowedRegisterFormExtension(registerForm.FileName))
                return Results.BadRequest(ApiResponse.Fail("Phiếu đăng ký phải là PDF, DOC hoặc DOCX."));

            using var registerFormStream = new MemoryStream();
            await registerForm.CopyToAsync(registerFormStream, cancellationToken);
            registerFormPdf = registerFormStream.ToArray();
        }

        if (body.Attachments is { Count: > 0 } attachments
            && !FileUploadValidator.TryValidate(attachments, NoteAttachmentMaxBytes, MaxProposalAttachments, out var attachmentError))
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
            MaxStudents: body.MaxStudents,
            RegisterFormPdf: registerFormPdf
        );

        var projectId = await sender.Send(command, cancellationToken);

        // Best-effort: quarantine + malware-scan the register form and attachments so they become viewable
        // topic documents. The topic already exists, so a queue failure only logs — it never fails the proposal.
        var scanResult = await ProposalUploadScanner.QueueAsync(
            scanWorkflow, projectId, currentUser.UserId!.Value, body.RegisterForm, body.Attachments, cancellationToken);
        if (!scanResult.Success)
            logger.LogWarning("Proposal {ProjectId}: queueing uploads for malware scan failed: {Error}", projectId, scanResult.ErrorMessage);

        return Created($"/api/topic-pools/topics/{projectId}", new { id = projectId }, "Đề xuất đề tài thành công.");
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
