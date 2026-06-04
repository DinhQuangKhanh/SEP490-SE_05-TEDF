using MediatR;
using TEDF.Application.Features.TopicPools.DTOs;
using TEDF.Application.Features.TopicPools.Commands.ConfirmRegistration;
using TEDF.Application.Features.TopicPools.Commands.ProposeTopicToPool;
using TEDF.Application.Features.TopicPools.Commands.RejectRegistration;
using TEDF.Application.Features.TopicPools.Commands.RequestRegistration;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolById;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPools;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolsByDepartment;
using TEDF.Application.Features.TopicPools.Queries.GetTopicPoolStatistics;
using TEDF.API.Endpoints.Topics.Requests;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Common.Security.Abstractions;
using TEDF.Application.Common.Interfaces;

namespace TEDF.API.Endpoints.Topics;

public sealed class TopicPoolsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var pool = app.MapGroup("/api/topic-pools").RequireAuthorization();

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
            .DisableAntiforgery()
            .WithTags("TopicPools")
            .WithName("ProposeTopicToPool")
            .Produces(201).Produces(400).Produces(401).Produces(404).Produces(503);

        pool.MapPost("/{groupId:guid}/topic-registrations", RequestTopicRegistration)
            .RequireAuthorization(PolicyNames.GroupLeader)
            .WithTags("TopicPools")
            .WithName("RequestTopicRegistration")
            .Produces(201).Produces(400).Produces(401).Produces(403);

        pool.MapPut("/registrations/{id:guid}/confirm", ConfirmTopicRegistration)
            .WithTags("TopicPools")
            .WithName("ConfirmTopicRegistration")
            .Produces(204).Produces(400).Produces(401).Produces(404);

        pool.MapPut("/registrations/{id:guid}/reject", RejectTopicRegistration)
            .WithTags("TopicPools")
            .WithName("RejectTopicRegistration")
            .Produces(204).Produces(400).Produces(401).Produces(404);
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
            MaxStudents: body.MaxStudents
        );

        var projectId = await sender.Send(command, cancellationToken);
        return Created($"/api/topic-pools/topics/{projectId}", new { id = projectId }, "Đề xuất đề tài thành công.");
    }

    private static async Task<IResult> RequestTopicRegistration(Guid groupId, TopicRegistrationRequest body, ISender sender, CancellationToken ct)
    {
        var registrationId = await sender.Send(new RequestTopicRegistrationCommand(body.ProjectId, groupId, body.Note), ct);
        return Created($"/api/topic-pools/registrations/{registrationId}", new { id = registrationId }, "Tạo mới thành công.");
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
}
