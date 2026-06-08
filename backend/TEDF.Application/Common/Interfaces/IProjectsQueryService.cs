using TEDF.Application.Features.Projects.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Projects feature. Query handlers depend on this only.
/// </summary>
public interface IProjectsQueryService
{
    Task<GetProjectsQueryResult> GetProjectsAsync(
        string? search, int? semesterId, string? status, int? majorId,
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Department-head: projects within the caller's department (resolves department internally).</summary>
    Task<DepartmentProjectsResponse> GetDepartmentProjectsAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
