using TEDF.Application.Features.Dashboard.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Dashboard feature — all per-role dashboards. Query handlers depend on this only.
/// </summary>
public interface IDashboardQueryService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
    Task<MentorDashboardDto> GetMentorDashboardAsync(Guid mentorId, CancellationToken cancellationToken = default);
    Task<DepartmentHeadDashboardDto> GetDepartmentHeadDashboardAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<EvaluatorDashboardDto> GetEvaluatorDashboardAsync(Guid evaluatorId, CancellationToken cancellationToken = default);
}
