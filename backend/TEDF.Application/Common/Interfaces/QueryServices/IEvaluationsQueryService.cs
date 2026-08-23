using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Evaluations.DTOs;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Common.Interfaces;

public interface IEvaluationsQueryService
{
    Task<EvaluatorFilterOptionsDto> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    Task<EvaluatorDashboardDto> GetDashboardAsync(Guid evaluatorId, CancellationToken cancellationToken = default);

    Task<EvaluatorHistoryDto> GetHistoryAsync(
        Guid evaluatorId,
        int page,
        int pageSize,
        string? search,
        string? result,
        string? dateRange,
        CancellationToken cancellationToken = default);

    Task<EvaluatorProjectsDto> GetProjectsAsync(
        Guid evaluatorId,
        int page,
        int pageSize,
        string? search,
        int? semesterId,
        int? majorId,
        string? result,
        CancellationToken cancellationToken = default);

    Task<ProjectReviewDetailDto?> GetProjectForReviewAsync(
        Guid projectId,
        Guid evaluatorId,
        CancellationToken cancellationToken = default);

    /// <summary>Department-head: evaluators in the caller's department (resolves department internally).</summary>
    Task<List<DepartmentEvaluatorDto>> GetDepartmentEvaluatorsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
