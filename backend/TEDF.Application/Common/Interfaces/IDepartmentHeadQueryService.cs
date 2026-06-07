using TEDF.Application.Features.Dashboard.DTOs;
using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Common.Interfaces;

public interface IDepartmentHeadQueryService
{
    Task<DepartmentProjectsResponse> GetDepartmentProjectsAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<List<DepartmentEvaluatorDto>> GetDepartmentEvaluatorsAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<DepartmentHeadDashboardDto> GetDashboardAsync(
        int departmentId,
        Guid headId,
        CancellationToken cancellationToken = default);
}
