using MediatR;
using Microsoft.AspNetCore.Mvc;
using TEDF.API.Endpoints.EvaluationChecklists.Requests;
using TEDF.Application.Common;
using TEDF.Application.Common.Interfaces;
using TEDF.Application.Features.EvaluationChecklists.Commands.ActivateChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.Commands.CopyChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.Commands.CreateChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.Commands.DeactivateChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.Commands.ImportChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.Commands.SaveProjectChecklist;
using TEDF.Application.Features.EvaluationChecklists.Commands.UpdateChecklistConfig;
using TEDF.Application.Features.EvaluationChecklists.DTOs;
using TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigById;
using TEDF.Application.Features.EvaluationChecklists.Queries.GetChecklistConfigs;
using TEDF.Application.Features.EvaluationChecklists.Queries.GetDefaultChecklistCriteria;
using TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectChecklist;
using TEDF.Application.Features.EvaluationChecklists.Queries.GetProjectEvaluatorChecklist;
using TEDF.Application.Features.EvaluationChecklists.Queries.PreviewChecklistImport;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.EvaluationChecklists;

/// <summary>
/// Endpoints for the topic-evaluation checklist:
/// - Evaluator: read / save (score) the checklist for a project they are assigned to.
/// - Department Head: manage the per-semester checklist configurations (Excel import + preview + template).
/// </summary>
public sealed class EvaluationChecklistEndpoints : IEndpoint
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string SwaggerTag = "EvaluationChecklists";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // ── Evaluator self-service (nested under the Evaluations feature group) ──
        var evaluator = app.MapGroup("/api/evaluations").RequireAuthorization();

        evaluator.MapGet("/projects/{projectId:guid}/checklist", GetProjectChecklist)
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags(SwaggerTag).WithName("GetProjectChecklist")
            .Produces<ApiResponse<ProjectChecklistDto>>().Produces(401).Produces(403).Produces(404);

        evaluator.MapPut("/projects/{projectId:guid}/checklist", SaveProjectChecklist)
            .RequireAuthorization(PolicyNames.RequireEvaluator)
            .WithTags(SwaggerTag).WithName("SaveProjectChecklist")
            .Produces<ApiResponse<string>>().Produces(400).Produces(401).Produces(403).Produces(404);

        // Department-Head read-only view of a specific evaluator's checklist (needs-decision / history).
        evaluator.MapGet("/projects/{projectId:guid}/evaluators/{evaluatorId:guid}/checklist", GetEvaluatorChecklistForReview)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags(SwaggerTag).WithName("GetEvaluatorChecklistForReview")
            .Produces<ApiResponse<ProjectChecklistDto>>().Produces(401).Produces(403).Produces(404);

        // ── Department-Head checklist configuration management ──
        var config = app.MapGroup("/api/checklist-configs")
            .RequireAuthorization(PolicyNames.RequireDepartmentHead);

        config.MapGet("", GetChecklistConfigs)
            .WithTags(SwaggerTag).WithName("GetChecklistConfigs")
            .Produces<ApiResponse<ChecklistConfigListDto>>().Produces(401).Produces(403);

        config.MapGet("/default-criteria", GetDefaultCriteria)
            .WithTags(SwaggerTag).WithName("GetDefaultChecklistCriteria")
            .Produces<ApiResponse<IReadOnlyList<ChecklistCriterionSeedDto>>>().Produces(401).Produces(403);

        config.MapGet("/template", DownloadTemplate)
            .WithTags(SwaggerTag).WithName("DownloadChecklistTemplate")
            .Produces(200).Produces(401).Produces(403);

        config.MapGet("/{id:guid}", GetChecklistConfigById)
            .WithTags(SwaggerTag).WithName("GetChecklistConfigById")
            .Produces<ApiResponse<ChecklistConfigDto>>().Produces(401).Produces(403).Produces(404);

        config.MapPost("/preview", PreviewImport).DisableAntiforgery()
            .WithTags(SwaggerTag).WithName("PreviewChecklistImport")
            .Produces<ApiResponse<ChecklistImportPreviewDto>>().Produces(400).Produces(401).Produces(403);

        config.MapPost("/import", ImportChecklistConfig).DisableAntiforgery()
            .WithTags(SwaggerTag).WithName("ImportChecklistConfig")
            .Produces<ApiResponse<Guid>>().Produces(400).Produces(401).Produces(403).Produces(404);

        config.MapPost("", CreateChecklistConfig)
            .WithTags(SwaggerTag).WithName("CreateChecklistConfig")
            .Produces<ApiResponse<Guid>>().Produces(400).Produces(401).Produces(403).Produces(404);

        config.MapPost("/{id:guid}/copy", CopyChecklistConfig)
            .WithTags(SwaggerTag).WithName("CopyChecklistConfig")
            .Produces<ApiResponse<Guid>>().Produces(400).Produces(401).Produces(403).Produces(404);

        config.MapPut("/{id:guid}", UpdateChecklistConfig)
            .WithTags(SwaggerTag).WithName("UpdateChecklistConfig")
            .Produces<ApiResponse<string>>().Produces(400).Produces(401).Produces(403).Produces(404);

        config.MapPost("/{id:guid}/activate", ActivateChecklistConfig)
            .WithTags(SwaggerTag).WithName("ActivateChecklistConfig")
            .Produces<ApiResponse<string>>().Produces(400).Produces(401).Produces(403).Produces(404);

        config.MapPost("/{id:guid}/deactivate", DeactivateChecklistConfig)
            .WithTags(SwaggerTag).WithName("DeactivateChecklistConfig")
            .Produces<ApiResponse<string>>().Produces(400).Produces(401).Produces(403).Produces(404);
    }

    // ── Evaluator handlers ──
    private static async Task<IResult> GetProjectChecklist(Guid projectId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectChecklistQuery(projectId), ct));

    private static async Task<IResult> SaveProjectChecklist(
        Guid projectId, [FromBody] SaveProjectChecklistRequest body, ISender sender, CancellationToken ct)
    {
        var items = (body.Items ?? [])
            .Select(i => new ChecklistScoreInput(i.CriterionId, i.Score, i.Comment))
            .ToList();
        await sender.Send(new SaveProjectChecklistCommand(projectId, items, body.Note), ct);
        return Ok("Đã lưu kết quả checklist.");
    }

    private static async Task<IResult> GetEvaluatorChecklistForReview(
        Guid projectId, Guid evaluatorId, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetProjectEvaluatorChecklistQuery(projectId, evaluatorId), ct));

    // ── Department-Head handlers ──
    private static async Task<IResult> GetChecklistConfigs(ISender sender, int? semesterId, CancellationToken ct)
        => Ok(await sender.Send(new GetChecklistConfigsQuery(semesterId), ct));

    private static async Task<IResult> GetDefaultCriteria(ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetDefaultChecklistCriteriaQuery(), ct));

    private static async Task<IResult> GetChecklistConfigById(Guid id, ISender sender, CancellationToken ct)
        => Ok(await sender.Send(new GetChecklistConfigByIdQuery(id), ct));

    private static IResult DownloadTemplate(IChecklistExcelService excel)
        => Results.File(excel.GenerateTemplate(), XlsxContentType, "checklist-tham-dinh-mau.xlsx");

    private static async Task<IResult> PreviewImport(IFormFile file, ISender sender, CancellationToken ct)
    {
        var bytes = await ReadAllBytesAsync(file, ct);
        var preview = await sender.Send(new PreviewChecklistImportQuery(bytes, file.FileName), ct);
        return Ok(preview, "Đã đọc nội dung file.");
    }

    private static async Task<IResult> ImportChecklistConfig(
        [FromForm] int semesterId, [FromForm] int requiredPassCount, IFormFile file, ISender sender, CancellationToken ct)
    {
        var bytes = await ReadAllBytesAsync(file, ct);
        var id = await sender.Send(new ImportChecklistConfigCommand(semesterId, bytes, file.FileName, requiredPassCount), ct);
        return Ok(id, "Đã import checklist (bản nháp).");
    }

    private static async Task<IResult> CreateChecklistConfig(
        [FromBody] CreateChecklistConfigRequest body, ISender sender, CancellationToken ct)
    {
        var id = await sender.Send(
            new CreateChecklistConfigCommand(body.SemesterId, MapCriteria(body.Criteria), body.RequiredPassCount), ct);
        return Ok(id, "Đã tạo checklist (bản nháp).");
    }

    private static async Task<IResult> CopyChecklistConfig(
        Guid id, [FromBody] CopyChecklistConfigRequest body, ISender sender, CancellationToken ct)
    {
        var newId = await sender.Send(new CopyChecklistConfigCommand(id, body.TargetSemesterId), ct);
        return Ok(newId, "Đã sao chép checklist.");
    }

    private static async Task<IResult> UpdateChecklistConfig(
        Guid id, [FromBody] UpdateChecklistConfigRequest body, ISender sender, CancellationToken ct)
    {
        await sender.Send(new UpdateChecklistConfigCommand(id, MapCriteria(body.Criteria), body.RequiredPassCount), ct);
        return Ok("Đã cập nhật checklist.");
    }

    private static async Task<IResult> ActivateChecklistConfig(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new ActivateChecklistConfigCommand(id), ct);
        return Ok("Đã kích hoạt checklist.");
    }

    private static async Task<IResult> DeactivateChecklistConfig(Guid id, ISender sender, CancellationToken ct)
    {
        await sender.Send(new DeactivateChecklistConfigCommand(id), ct);
        return Ok("Đã ngừng sử dụng checklist.");
    }

    private static List<ChecklistCriterionInput> MapCriteria(IReadOnlyList<ChecklistCriterionRequest>? criteria)
        => (criteria ?? [])
            .Select(c => new ChecklistCriterionInput(c.TitleVi, c.TitleEn, c.Description, c.MaxScore, c.PassScore))
            .ToList();

    private static async Task<byte[]> ReadAllBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
