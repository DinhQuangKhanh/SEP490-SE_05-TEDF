using MediatR;
using TEDF.API.Endpoints.DepartmentHeads.Requests;
using TEDF.Application.Features.Departments.Commands.AssignEvaluator;
using TEDF.Application.Features.Departments.Commands.SubmitFinalDecision;
using TEDF.Infrastructure.Authorization.Policies;
using static TEDF.API.Extensions.ApiResponseExtensions;

namespace TEDF.API.Endpoints.DepartmentHeads;

public partial class DepartmentHeadEndpoints : IEndpoint
{
    private static void MapCommandEndpoints(RouteGroupBuilder group)
    {
        // ─────────────────────────────────────────────────────────────
        // Commands: các endpoint làm thay đổi dữ liệu/state
        // ─────────────────────────────────────────────────────────────

        #region Gán người thẩm định cho đề tài

        // POST /api/department-head/assign-evaluator
        // Chủ nhiệm bộ môn gán một người thẩm định vào đề tài theo thứ tự.
        group.MapPost("assign-evaluator", AssignEvaluator)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("AssignEvaluator")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404);

        #endregion

        #region Gửi quyết định cuối cùng cho đề tài

        // POST /api/department-head/projects/{projectId}/final-decision
        // Chủ nhiệm bộ môn đưa ra quyết định cuối cùng khi hai người thẩm định bất đồng.
        group.MapPost("projects/{projectId:guid}/final-decision", SubmitFinalDecision)
            .RequireAuthorization(PolicyNames.DepartmentHeadOfDepartment)
            .WithTags("DepartmentHead")
            .WithName("SubmitFinalDecision")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        #endregion
    }

    #region Handler: gán người thẩm định cho đề tài

    private static async Task<IResult> AssignEvaluator(AssignEvaluatorRequest request, ISender sender, CancellationToken ct)
    {
        var command = new AssignEvaluatorCommand(
            request.ProjectId,
            request.EvaluatorId,
            request.EvaluatorOrder);

        await sender.Send(command, ct);
        return NoContent("Gán người thẩm định thành công.");
    }

    #endregion

    #region Handler: gửi quyết định cuối cùng cho đề tài

    private static async Task<IResult> SubmitFinalDecision(
        Guid projectId, SubmitFinalDecisionRequest request, ISender sender, CancellationToken ct)
    {
        var command = new SubmitFinalDecisionCommand(projectId, request.Result, request.Notes);
        await sender.Send(command, ct);
        return NoContent("Quyết định cuối cùng đã được gửi thành công.");
    }

    #endregion
}
