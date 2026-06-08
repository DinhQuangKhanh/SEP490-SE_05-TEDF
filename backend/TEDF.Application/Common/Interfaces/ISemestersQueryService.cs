using TEDF.Application.Features.Semesters.DTOs;

namespace TEDF.Application.Common.Interfaces;

/// <summary>
/// Read-side service for the Semesters feature.
/// Query handlers depend on this service only.
/// </summary>
public interface ISemestersQueryService
{
    Task<SemesterDto?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<SemesterDto>> GetAllAsync(string? status, CancellationToken cancellationToken = default);
    Task<SemesterDto> GetByIdAsync(int semesterId, CancellationToken cancellationToken = default);
}
